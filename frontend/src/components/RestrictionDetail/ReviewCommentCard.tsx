import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { MessageSquareWarning } from "lucide-react";

export function ReviewCommentCard({ comment }: { comment: string }) {
  return (
    <section
      data-testid="review-comment-callout"
      aria-label={t.requestedChangesComment}
      className="rounded-lg border-2 border-l-8 border-[#EF7D00] bg-[#FEF3C7] p-4 font-['Inter_Variable','Inter',sans-serif] shadow-md"
    >
      <h2 className="flex items-center gap-2 text-lg font-bold text-[#EF7D00]">
        <MessageSquareWarning className="size-6 shrink-0" aria-hidden />
        {t.requestedChangesComment}
      </h2>
      <p
        data-testid="review-comment"
        className="mt-2 text-[20px] font-semibold whitespace-pre-wrap text-black"
      >
        {comment}
      </p>
    </section>
  );
}
