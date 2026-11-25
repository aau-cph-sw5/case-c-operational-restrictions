import type { ReactNode } from "react";

type AppHeaderProps = {
  title: string;
  subtitle?: string;
  children?: ReactNode;
};

export function AppHeader({ title, subtitle, children }: AppHeaderProps) {
  return (
    <header className="flex shrink-0 flex-col gap-4 border-b border-border/50 bg-[#E0F3FF] px-4 pb-4 pt-5">
      <div className="text-center">
        <h1 className="font-heading text-xl font-bold text-foreground md:text-2xl">
          {title}
        </h1>

        {subtitle && (
          <p className="mt-1 text-sm text-[#6B6B6B] md:text-base">{subtitle}</p>
        )}
      </div>

      {children}
    </header>
  );
}
