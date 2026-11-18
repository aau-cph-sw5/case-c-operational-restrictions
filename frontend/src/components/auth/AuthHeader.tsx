interface AuthHeaderProps {
  subtitle?: string;
}

export function AuthHeader({
  subtitle = "Driftsrestriktioner",
}: AuthHeaderProps) {
  return (
    <header className="text-center space-y-1">
      <img
        src="/pwa-192x192.png"
        alt="Metro Service logo"
        className="size-12 rounded-xl shadow-xs mx-auto mb-2"
      />
      <h1 className="text-2xl font-bold tracking-tight text-foreground font-ui">
        Metro Service
      </h1>
      <p className="text-sm text-muted-foreground">{subtitle}</p>
    </header>
  );
}
