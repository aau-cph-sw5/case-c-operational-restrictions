import { ErrorAlert } from "@/components/common/ErrorAlert.tsx";
import { TemporaryBackButton } from "@/components/common/TemporaryBackButton.tsx";
import { buttonVariants } from "@/components/ui/button.tsx";
import { pageClasses } from "@/lib/pageLayout.ts";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { Link } from "react-router-dom";

interface ErrorPageProps {
  message: string;
  backTo?: string | null;
  backLabel?: string;
}

export function ErrorPage({
  message,
  backTo = "/home",
  backLabel = t.backToHome,
}: ErrorPageProps) {
  return (
    <div className={pageClasses}>
      <TemporaryBackButton />
      <ErrorAlert>{message}</ErrorAlert>
      {backTo && (
        <Link
          to={backTo}
          className={buttonVariants({
            variant: "link",
            className:
              "self-start font-['Inter_Variable','Inter',sans-serif] text-base text-[#2563EB]",
          })}
        >
          {backLabel}
        </Link>
      )}
    </div>
  );
}
