import MessageUI
import SwiftUI

struct ContentView: View {
    @Environment(\.scenePhase) private var phase
    @AppStorage("gatewayEndpoint") private var endpoint = ""
    @AppStorage("keepConnected") private var keepConnected = false
    @AppStorage("pendingResult") private var pendingResult = Data()
    @State private var host = ""
    @State private var token = ""
    @State private var deviceName = ""
    @State private var connected = false
    @State private var problem = ""
    @State private var latestSync: Date?
    @State private var job: Job?
    @State private var composing: Job?
    @State private var claiming = false

    private var gateway: Gateway? {
        let value = host.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let parts = URLComponents(string: value), let url = parts.url, let domain = parts.host,
              parts.user == nil, parts.password == nil, parts.query == nil, parts.fragment == nil,
              parts.path.isEmpty || parts.path == "/",
              parts.scheme == "https" || (parts.scheme == "http" && domain == "localhost"),
              token.count == 43 else { return nil }
        return Gateway(url: url, token: token)
    }

    var body: some View {
        NavigationView {
            Form {
                if !connected {
                    Section("连接设备") {
                        TextField("https://sms.example.com", text: $host)
                            .keyboardType(.URL)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                        SecureField("设备凭据", text: $token)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                        Button("连接") {
                            guard gateway != nil else {
                                problem = "请输入 HTTPS 服务器地址和设备凭据。"
                                return
                            }
                            if !pendingResult.isEmpty && (host != endpoint || token != DeviceKey.read()) {
                                problem = "上次发送结果尚未回报，请先使用原设备连接。"
                                return
                            }
                            guard DeviceKey.save(token) else {
                                problem = "设备凭据无法保存。"
                                return
                            }
                            endpoint = host
                            problem = ""
                            keepConnected = true
                            connected = true
                        }
                    }
                } else {
                    Section("设备状态") {
                        statusRow("设备", value: deviceName.isEmpty ? "正在连接…" : deviceName)
                        statusRow("最近同步", value: latestSync?.formatted(date: .omitted, time: .standard) ?? "尚未同步")
                        if !pendingResult.isEmpty { Label("等待回报上次发送结果", systemImage: "clock") }
                        Button("断开连接", role: .destructive) {
                            keepConnected = false
                            connected = false
                            job = nil
                            deviceName = ""
                            latestSync = nil
                            problem = ""
                        }
                        .disabled(claiming || composing != nil)
                    }
                    Section {
                        if let job {
                            statusRow("收件人", value: job.recipient)
                            Text(job.body).textSelection(.enabled)
                            Button("打开系统短信并确认发送") { Task { await claim(job) } }
                                .disabled(claiming || composing != nil || !pendingResult.isEmpty || !MFMessageComposeViewController.canSendText())
                        } else {
                            Text("当前没有待发送任务").foregroundStyle(.secondary)
                        }
                    } header: {
                        Text("短信任务")
                    } footer: {
                        Text("请核对系统短信中的收件人与正文，保持设备前台以接收任务。")
                    }
                }
                if !problem.isEmpty {
                    Section { Label(problem, systemImage: "exclamationmark.circle").foregroundStyle(.red) }
                }
            }
            .navigationTitle("联信设备")
        }
        .navigationViewStyle(StackNavigationViewStyle())
        .onAppear {
            if host.isEmpty { host = endpoint }
            if token.isEmpty { token = DeviceKey.read() }
            if keepConnected && gateway != nil { connected = true }
            UIApplication.shared.isIdleTimerDisabled = connected && phase == .active
        }
        .onChange(of: connected && phase == .active) { active in
            UIApplication.shared.isIdleTimerDisabled = active
        }
        .task(id: connected && phase == .active) {
            if connected && phase == .active { await monitor() }
        }
        .sheet(item: $composing) { job in
            MessageComposer(job: job) { status in
                if let status {
                    pendingResult = (try? JSONEncoder().encode(JobResult(id: job.id, status: status))) ?? Data()
                }
                composing = nil
            }
            .ignoresSafeArea()
            .interactiveDismissDisabled()
        }
    }

    private func statusRow(_ title: String, value: String) -> some View {
        HStack {
            Text(title)
            Spacer()
            Text(value)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.trailing)
        }
    }

    private func monitor() async {
        guard let gateway else { return }
        while !Task.isCancelled {
            do {
                if let result = try? JSONDecoder().decode(JobResult.self, from: pendingResult) {
                    _ = try await gateway.request("report", body: result, as: Acknowledgment.self)
                    pendingResult = Data()
                }
                let canSend = MFMessageComposeViewController.canSendText()
                let data = try await gateway.request("poll", body: ["canSend": canSend], as: Poll.self)
                deviceName = data.device.name
                if composing == nil && !claiming { job = data.job }
                for item in data.sync {
                    _ = try await gateway.request("sync", body: ["id": item.id], as: Acknowledgment.self)
                    latestSync = .now
                }
                problem = canSend ? "" : "此设备当前无法发送系统短信。"
            } catch {
                if Task.isCancelled { return }
                problem = error.localizedDescription
            }
            try? await Task.sleep(nanoseconds: 2_000_000_000)
        }
    }

    private func claim(_ task: Job) async {
        guard !claiming, let gateway else { return }
        claiming = true
        defer { claiming = false }
        do {
            _ = try await gateway.request("claim", body: ["id": task.id], as: Acknowledgment.self)
            job = nil
            if MFMessageComposeViewController.canSendText() {
                composing = task
            } else {
                pendingResult = try JSONEncoder().encode(JobResult(id: task.id, status: "failed"))
            }
        } catch {
            problem = error.localizedDescription
        }
    }
}

