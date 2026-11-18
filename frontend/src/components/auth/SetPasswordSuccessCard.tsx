import { useNavigate } from "react-router-dom";
import { CheckCircle2 } from "lucide-react";
import { Button } from "../ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { isStandaloneDisplayMode } from "../../hooks/usePwaInstall";

export function SetPasswordSuccessCard() {
  const navigate = useNavigate();
  const isStandalone = isStandaloneDisplayMode();

  return (
    <Card className="border border-border/80 shadow-sm rounded-2xl">
      <CardHeader className="space-y-1 text-center">
        <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-green-100 text-green-600 dark:bg-green-950/40 dark:text-green-400 mb-2">
          <CheckCircle2 className="size-7" />
        </div>
        <CardTitle className="text-xl">Adgangskode oprettet</CardTitle>
        <CardDescription>
          {isStandalone
            ? "Din adgangskode er nu gemt. Du kan fortsætte direkte til login."
            : "Din nye adgangskode er oprettet. Du kan nu lukke denne fane og vende tilbage til Metro-appen for at logge ind."}
        </CardDescription>
      </CardHeader>

      <CardContent className="text-sm text-muted-foreground text-center">
        {isStandalone ? (
          <p>Log ind for at få adgang til driftsrestriktionerne.</p>
        ) : (
          <p>
            Hvis du ønsker at fortsætte i denne browser, kan du også logge ind
            her.
          </p>
        )}
      </CardContent>

      <CardFooter className="flex flex-col gap-2 pt-2">
        {isStandalone ? (
          <Button
            type="button"
            className="w-full cursor-pointer"
            onClick={() => navigate("/login")}
          >
            Gå til login
          </Button>
        ) : (
          <Button
            type="button"
            variant="outline"
            className="w-full cursor-pointer"
            onClick={() => navigate("/login")}
          >
            Log ind i browseren
          </Button>
        )}
      </CardFooter>
    </Card>
  );
}
