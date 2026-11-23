<!-- 来源：https://www.laoguicom.top/doc/473/ · 抓取状态：ok -->

## 淘宝时间戳经常挂 ,整理常用的时间戳备用
代码全部为EC get请求自行参考.

```js
// 北京时间戳API
let rel = http.httpGet("https://time.is/t/?zh.0.347.2464.0p.480.43d.1574683214663.1594044507830.",null,5*1000,{});
logd(rel.substring(0,13));// 时间戳 13位结果

//淘宝时间戳API
let rel = http.httpGet("http://api.m.taobao.com/rest/api3.do?api=mtop.common.getTimestamp",null,5*1000,{});
logd(JSON.parse(rel)["data"]["t"] );// 时间戳 13位结果

//苏宁时间戳API
let rel = http.httpGet("http://quan.suning.com/getSysTime.do",null,5*1000,{});
logd(JSON.parse(rel)["sysTime1"] ); // YYYY--MM-DD HH-mm-ss 格式时间 例:2023-02-04 01:25:21
logd(JSON.parse(rel)["sysTime2"] ); // 苏宁时间戳 上面的格式去掉纯数字结果 例:20230204012521

// 拼多多时间戳API
let rel = http.httpGet("http://api.pinduoduo.com/api/server/_stm",null,5*1000,{});
logd(JSON.parse(rel)["server_time"] ); // 时间戳 13位结果


// 物美多点时间戳API
let rel = http.httpGetDefault("https://appapis.dmall.com/static/queryUserCheckInfo.jsonp", 5*1000,{});
logd(rel.substring(63,76)); // 时间戳 13位结果

// 小米有品时间戳API
let rel = http.httpGetDefault("https://tptm.hd.mi.com/gettimestamp", 5*1000,{});
logd(rel.split("=")[1]); // 时间戳 10位结果


// 国美时间戳API
let rel = http.httpGetDefault("https://im-platform.gome.com.cn/im-platform/system/serverTime.json?appId=gomeplus_pro", 5*1000,{});
logd(JSON.parse(rel)["data"]["time"] ); // 时间戳 13位结果

// vivo官网时间戳API
let rel = http.httpGetDefault("http://mshopact.vivo.com.cn/tool/config", 5*1000,{});
logd(JSON.parse(rel)["data"]["nowTime"] ); // 时间戳 13位结果

// 魅族官网时间戳API
let rel = http.httpGetDefault("https://book.meizu.com/gettime.js", 5*1000,{});
logd(rel.substring(16,26)); // 时间戳 10位结果

//QQ时间戳API以及获取IP地址
let rel = http.httpGetDefault("http://vv6.video.qq.com/checktime", 5 * 1000, {"randnum":"0.45350576033883383",
        "guid":"0f5d85d631236102",
        "otype":"json"});
logd(rel.match(/\d+(.\d+)?/g)[2]); // 时间戳 10位结果

```