import Foundation

private struct GatewayError: LocalizedError {
    let message: String
    var errorDescription: String? { message }
}
private final class RejectRedirects: NSObject, URLSessionTaskDelegate, Sendable {
    func urlSession(
        _ session: URLSession,
        task: URLSessionTask,
        willPerformHTTPRedirection response: HTTPURLResponse,
        newRequest request: URLRequest,
        completionHandler: @escaping @Sendable (URLRequest?) -> Void
    ) {
        completionHandler(nil)
    }
}
struct Gateway {
    let url: URL
    let token: String
    private static let session = URLSession(
        configuration: .ephemeral, delegate: RejectRedirects(), delegateQueue: nil
    )

    func request<T: Decodable>(_ action: String, body: some Encodable, as type: T.Type) async throws -> T {
        var request = URLRequest(url: url.appendingPathComponent("api/device/\(action)"))
        request.httpMethod = "POST"
        request.timeoutInterval = 8
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(body)
        let (data, response) = try await Self.session.data(for: request)
        guard let http = response as? HTTPURLResponse else {
            throw GatewayError(message: "服务器响应无效。")
        }
        guard (200..<300).contains(http.statusCode) else {
            throw GatewayError(message: (try? JSONDecoder().decode(Failure.self, from: data))?.error ?? "请求失败（\(http.statusCode)）。")
        }
        return try JSONDecoder().decode(type, from: data)
    }
}

