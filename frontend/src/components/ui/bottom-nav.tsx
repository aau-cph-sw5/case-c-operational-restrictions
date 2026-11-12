import { Bell, PlusCircle, ListTodo, Settings } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "@/lib/utils";

export function BottomNav() {
  const location = useLocation();
  const currentPath = location.pathname;

  const navItems = [
    { label: "Notifikationer", icon: Bell, path: "/notifications" },
    { label: "Opret DR", icon: PlusCircle, path: "/restrictions/new" },
    { label: "DR Liste", icon: ListTodo, path: "/" },
    { label: "Indstillinger", icon: Settings, path: "/settings" },
  ];

  return (
    <nav className="absolute bottom-0 left-0 right-0 z-50 bg-[#E0F3FF] border-t border-border/60 px-2 py-2 shadow-lg">
      <div className="flex items-center justify-around max-w-md mx-auto">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive =
            currentPath === item.path ||
            (item.path === "/" && currentPath === "/overview");

          return (
            <Link
              key={item.label}
              to={item.path}
              className={cn(
                "flex flex-col items-center gap-1 py-1 px-3 rounded-xl transition-colors relative group",
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
              <span className="text-[11px] font-medium leading-none">
                {item.label}
              </span>
              {/* Active navigation indicator dot using #2563EB */}
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
