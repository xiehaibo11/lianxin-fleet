import MessageUI
import SwiftUI

struct MessageComposer: UIViewControllerRepresentable {
    let job: Job
    let onResult: (String?) -> Void

    func makeCoordinator() -> Coordinator { Coordinator(onResult: onResult) }
    func makeUIViewController(context: Context) -> MFMessageComposeViewController {
        let controller = MFMessageComposeViewController()
        controller.recipients = [job.recipient]
        controller.body = job.body
        controller.messageComposeDelegate = context.coordinator
        return controller
    }
    func updateUIViewController(_ controller: MFMessageComposeViewController, context: Context) {}

    final class Coordinator: NSObject, MFMessageComposeViewControllerDelegate {
        let onResult: (String?) -> Void
        init(onResult: @escaping (String?) -> Void) { self.onResult = onResult }

        func messageComposeViewController(_ controller: MFMessageComposeViewController, didFinishWith result: MessageComposeResult) {
            switch result {
            case .sent: onResult("submitted")
            case .cancelled, .failed: onResult("failed")
            @unknown default: onResult(nil)
            }
        }
    }
}
