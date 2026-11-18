import { Link } from "react-router-dom";
import { AlertCircle } from "lucide-react";
import { buttonVariants } from "../ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "../ui/card";

interface InvalidTokenCardProps {
  errorMessage: string | null;
}

export function InvalidTokenCard({ errorMessage }: InvalidTokenCardProps) {
  return (
    <Card className="border border-border/80 shadow-sm rounded-2xl">
      <CardHeader className="space-y-1 text-center">
        <div className="mx-auto flex size-10 items-center justify-center rounded-full bg-destructive/10 text-destructive mb-1">
          <AlertCircle className="size-5" />
        </div>
        <CardTitle className="text-xl">Ugyldigt eller udløbet link</CardTitle>
        <CardDescription>
          {errorMessage ||
            "Dette link til oprettelse af adgangskode er ikke længere gyldigt."}
        </CardDescription>
      </CardHeader>
      <CardContent className="text-sm text-muted-foreground text-center">
        Du kan anmode om et nyt nulstillingslink fra login-siden.
      </CardContent>
      <CardFooter className="pt-2">
        <Link
          to="/login"
          className={buttonVariants({
            className: "w-full cursor-pointer text-center",
          })}
        >
          Gå til login
        </Link>
      </CardFooter>
    </Card>
  );
}
