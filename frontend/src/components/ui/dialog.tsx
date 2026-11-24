import { Dialog as DialogPrimitive } from "@base-ui/react/dialog";
import { cn } from "cn";
import type { ComponentProps } from "react";

// Thin styled wrappers around Base UI's Dialog, in the same style as the other ui/ components.
// Mobile first: full screen on phones (header / scrollable body / footer),
// a centred modal from the sm breakpoint and up.
const Dialog = DialogPrimitive.Root;
const DialogClose = DialogPrimitive.Close;

// "screen": full screen on phones, centred modal from sm and up.
// "alert": a small centred box on every screen size, for confirmations.
const contentVariants = {
  screen:
    "inset-0 flex flex-col bg-[#F8FAFC] sm:inset-auto sm:top-1/2 sm:left-1/2 sm:max-h-[90svh] sm:w-[calc(100%-2rem)] sm:max-w-2xl sm:-translate-x-1/2 sm:-translate-y-1/2 sm:overflow-hidden sm:rounded-lg sm:shadow-lg",
  alert:
    // #DFE1E3 = the app background #F8FAFC made 10% darker (RGB × 0.9).
    "top-1/2 left-1/2 flex w-[calc(100%-2rem)] max-w-sm -translate-x-1/2 -translate-y-1/2 flex-col gap-4 rounded-lg bg-[#DFE1E3] p-5 shadow-lg",
} as const;

function DialogContent({
  className,
  children,
  variant = "screen",
  ...props
}: ComponentProps<typeof DialogPrimitive.Popup> & {
  variant?: keyof typeof contentVariants;
}) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Backdrop
        data-slot="dialog-backdrop"
        // Base UI skips the backdrop for nested dialogs; an alert must still dim what is behind it.
        forceRender={variant === "alert"}
        className="fixed inset-0 z-[1100] bg-black/50"
      />
      <DialogPrimitive.Popup
        data-slot="dialog-content"
        className={cn(
          // z-[1100]: above InstallButton (z-index 1000), so nothing sits on top of a dialog.
          "fixed z-[1100] outline-none",
          contentVariants[variant],
          className,
        )}
        {...props}
      >
        {children}
      </DialogPrimitive.Popup>
    </DialogPrimitive.Portal>
  );
}

// Stays at the top; leaves room for the notch / status bar.
function DialogHeader({ className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-header"
      className={cn(
        "shrink-0 space-y-1 border-b border-[#E5E7EB] bg-white px-4 pt-[calc(1rem+env(safe-area-inset-top))] pb-4 sm:pt-4",
        className,
      )}
      {...props}
    />
  );
}

// The only part that scrolls, so header and footer stay visible.
function DialogBody({ className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-body"
      className={cn(
        "min-h-0 flex-1 space-y-3 overflow-y-auto overscroll-contain px-4 py-4",
        className,
      )}
      {...props}
    />
  );
}

// Stays at the bottom; leaves room for the home indicator.
function DialogFooter({ className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-footer"
      className={cn(
        "shrink-0 space-y-2 border-t border-[#E5E7EB] bg-white px-4 pt-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))] sm:pb-3",
        className,
      )}
      {...props}
    />
  );
}

function DialogTitle({
  className,
  ...props
}: ComponentProps<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title
      data-slot="dialog-title"
      className={cn(
        "font-['Instrument_Sans',sans-serif] text-xl leading-tight font-bold text-black",
        className,
      )}
      {...props}
    />
  );
}

function DialogDescription({
  className,
  ...props
}: ComponentProps<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      data-slot="dialog-description"
      className={cn("text-base text-[#6B6B6B]", className)}
      {...props}
    />
  );
}

export {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
};
