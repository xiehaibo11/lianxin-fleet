import Foundation

struct Job: Codable, Identifiable {
    let id: Int
    let body: String
    let recipient: String
}
struct Device: Decodable { let name: String }
struct CheckIn: Decodable { let id: Int }
struct Poll: Decodable {
    let device: Device
    let job: Job?
    let sync: [CheckIn]
}
struct Acknowledgment: Decodable { let ok: Bool }
struct Failure: Decodable { let error: String }
struct JobResult: Codable {
    let id: Int
    let status: String
}
