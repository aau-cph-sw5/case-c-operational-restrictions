
// COMPONENT:
// Reusable UI building block for displaying notification delivery status

interface NotificationStatusProps {
  status: "pending" | "sent" | "failed";
}

const statusLabels = {
  pending: "Pending",
  sent: "Sent",
  failed: "Failed",
};

export function NotificationStatus({
  status,
}: NotificationStatusProps) {
  return <span>{statusLabels[status]}</span>;
}
