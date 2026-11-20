import { Bell, PlusCircle, ListTodo, Settings } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "@/lib/utils";

export function BottomNav() {
  const location = useLocation();
  const currentPath = location.pathname;

  const navItems = [
    { label: "Notifikationer", icon: Bell, path: "/notifications" },
    { label: "Opret DR", icon: PlusCircle, path: "/restrictions/new" },
    { label: "DR Liste", icon: ListTodo, path: "/restrictions" },
    { label: "Indstillinger", icon: Settings, path: "/settings" },
  ];

  return (
    <nav
      aria-label="Hovednavigation"
      className="sticky bottom-0 z-50 border-t border-border/60 bg-[#E0F3FF] px-2 py-2 pb-[calc(0.5rem+env(safe-area-inset-bottom))] shadow-lg"
    >
      <div className="mx-auto flex w-full max-w-xl items-center justify-around">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = currentPath === item.path;

          return (
            <Link
              key={item.label}
              to={item.path}
              className={cn(
                "relative group flex min-w-0 flex-1 flex-col items-center justify-center gap-1 rounded-xl px-1 py-2 text-center transition-colors",
                isActive
                  ? "text-[#2563EB]"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              <Icon
                className={cn(
                  "size-6 transition-transform group-hover:scale-105",
                  isActive && "stroke-[2.5px]",
                )}
              />
              <span className="max-w-full text-[10px] font-medium leading-tight sm:text-xs">
                {item.label}
              </span>
              {isActive && (
                <span className="absolute -bottom-1 size-1 rounded-full bg-[#2563EB]" />
              )}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
