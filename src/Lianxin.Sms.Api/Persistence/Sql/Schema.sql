PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;
CREATE TABLE IF NOT EXISTS app_config (
  singleton INTEGER PRIMARY KEY CHECK(singleton = 1),
  mode TEXT NOT NULL CHECK(mode IN ('demo','iphone'))
);
CREATE TABLE IF NOT EXISTS tenants (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY,
  tenant_id INTEGER REFERENCES tenants(id),
  email TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  password TEXT NOT NULL,
  quota INTEGER NOT NULL DEFAULT 0 CHECK(quota >= 0),
  CHECK(tenant_id IS NOT NULL OR quota = 0)
);
CREATE TABLE IF NOT EXISTS sessions (
  token TEXT PRIMARY KEY,
  user_id INTEGER NOT NULL REFERENCES users(id),
  expires_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS lamp_user_links (
  lamp_user_id INTEGER PRIMARY KEY CHECK(lamp_user_id > 0),
  sms_user_id INTEGER NOT NULL UNIQUE REFERENCES users(id)
);
CREATE TABLE IF NOT EXISTS devices (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  mode TEXT NOT NULL CHECK(mode IN ('demo','iphone')),
  line TEXT UNIQUE,
  token TEXT UNIQUE,
  last_seen INTEGER,
  can_send INTEGER NOT NULL DEFAULT 0 CHECK(can_send IN (0, 1)),
  last_assigned INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS devices_assignment ON devices(can_send, last_assigned, id);
CREATE TABLE IF NOT EXISTS tenant_devices (
  device_id INTEGER PRIMARY KEY REFERENCES devices(id),
  tenant_id INTEGER NOT NULL REFERENCES tenants(id)
);
CREATE INDEX IF NOT EXISTS tenant_devices_tenant ON tenant_devices(tenant_id);
CREATE TABLE IF NOT EXISTS conversations (
  id INTEGER PRIMARY KEY,
  user_id INTEGER NOT NULL REFERENCES users(id),
  device_id INTEGER NOT NULL REFERENCES devices(id),
  recipient TEXT NOT NULL,
  last_read_id INTEGER NOT NULL DEFAULT 0,
  last_message_id INTEGER,
  UNIQUE(user_id, recipient),
  UNIQUE(device_id, recipient)
);
CREATE TABLE IF NOT EXISTS messages (
  id INTEGER PRIMARY KEY,
  conversation_id INTEGER NOT NULL REFERENCES conversations(id),
  direction TEXT NOT NULL CHECK(direction IN ('out','in')),
  body TEXT NOT NULL,
  status TEXT NOT NULL CHECK(status IN ('queued','sending','unknown','submitted','failed','received')),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS messages_conversation ON messages(conversation_id, id);
CREATE INDEX IF NOT EXISTS messages_queue ON messages(status, id) WHERE status IN ('queued','sending');
CREATE TABLE IF NOT EXISTS send_requests (
  user_id INTEGER NOT NULL REFERENCES users(id),
  request_key TEXT NOT NULL,
  fingerprint TEXT NOT NULL,
  result TEXT NOT NULL,
  PRIMARY KEY(user_id, request_key)
);
CREATE TABLE IF NOT EXISTS inbound_events (
  device_id INTEGER NOT NULL REFERENCES devices(id),
  event_id TEXT NOT NULL,
  message_id INTEGER NOT NULL REFERENCES messages(id),
  PRIMARY KEY(device_id, event_id)
);
CREATE TABLE IF NOT EXISTS quota_entries (
  id INTEGER PRIMARY KEY,
  user_id INTEGER NOT NULL REFERENCES users(id),
  delta INTEGER NOT NULL,
  reason TEXT NOT NULL,
  reference_id INTEGER,
  created_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS sync_batches (
  id INTEGER PRIMARY KEY,
  run_at INTEGER NOT NULL,
  deadline INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS sync_targets (
  batch_id INTEGER NOT NULL REFERENCES sync_batches(id),
  device_id INTEGER NOT NULL REFERENCES devices(id),
  confirmed_at INTEGER,
  PRIMARY KEY(batch_id, device_id)
);
CREATE INDEX IF NOT EXISTS sync_targets_device ON sync_targets(device_id, batch_id);
CREATE TABLE IF NOT EXISTS fleet_devices (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  udid TEXT NOT NULL UNIQUE,
  wda_port INTEGER NOT NULL UNIQUE CHECK(wda_port BETWEEN 1024 AND 65535),
  group_name TEXT NOT NULL DEFAULT '未分组',
  width INTEGER NOT NULL DEFAULT 390,
  height INTEGER NOT NULL DEFAULT 844,
  last_seen INTEGER
);
CREATE TABLE IF NOT EXISTS fleet_jobs (
  id INTEGER PRIMARY KEY,
  created_by INTEGER NOT NULL REFERENCES users(id),
  request_key TEXT NOT NULL,
  fingerprint TEXT NOT NULL,
  action TEXT NOT NULL CHECK(action IN ('tap','swipe','home','text','launch','install')),
  params TEXT NOT NULL,
  status TEXT NOT NULL CHECK(status IN ('queued','running','succeeded','partial','failed','unknown','cancelled')),
  created_at INTEGER NOT NULL,
  finished_at INTEGER,
  UNIQUE(created_by, request_key)
);
CREATE TABLE IF NOT EXISTS fleet_job_targets (
  job_id INTEGER NOT NULL REFERENCES fleet_jobs(id),
  device_id INTEGER NOT NULL REFERENCES fleet_devices(id),
  status TEXT NOT NULL CHECK(status IN ('queued','running','succeeded','failed','unknown','cancelled')),
  error TEXT,
  PRIMARY KEY(job_id, device_id)
);
CREATE INDEX IF NOT EXISTS fleet_targets_queue ON fleet_job_targets(status, job_id);
