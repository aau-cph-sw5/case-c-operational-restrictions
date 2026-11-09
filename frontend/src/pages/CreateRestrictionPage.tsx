// PAGE:
// Trin-for-trin formular til at oprette en kladde af en driftsrestriktion.
import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight, Check, Pencil } from "lucide-react";
import { cn } from "cn";
import { Alert, AlertDescription, AlertTitle } from "../components/ui/alert";
import { Button } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Label } from "../components/ui/label";
import { Textarea } from "../components/ui/textarea";
import { authService } from "../services/authService";
import {
  RestrictionValidationError,
  createRestriction,
} from "../services/restrictionApi";
import { FIELDS, STEPS } from "../lib/restrictionFields";
import {
  DATE_RANGE_ERROR,
  EMPTY_VALUES,
  hasErrors,
  validateRestrictionForm,
} from "../lib/restrictionValidation";
import type {
  FormErrors,
  RestrictionField,
  RestrictionFormValues,
} from "../lib/restrictionValidation";

export function CreateRestrictionPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [values, setValues] = useState<RestrictionFormValues>(EMPTY_VALUES);
  const [errors, setErrors] = useState<FormErrors>({});
  const [serverErrors, setServerErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const isLastStep = step === STEPS.length - 1;
  const stepFields = FIELDS.filter((field) => field.step === step);

  function handleChange(field: RestrictionField) {
    return (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const nextValue = e.target.value;
      setValues((prev) => ({ ...prev, [field]: nextValue }));
      setErrors((prev) => {
        const nextErrors = { ...prev, [field]: undefined };

        if (
          (field === "startDate" || field === "endDate") &&
          prev.endDate === DATE_RANGE_ERROR
        ) {
          const nextStartDate =
            field === "startDate" ? nextValue : values.startDate;
          const nextEndDate = field === "endDate" ? nextValue : values.endDate;

          if (!nextStartDate || !nextEndDate || nextEndDate > nextStartDate) {
            nextErrors.endDate = undefined;
          } else {
            nextErrors.endDate = DATE_RANGE_ERROR;
          }
        }

        return nextErrors;
      });
    };
  }

  function handleTabClick(targetStep: number) {
    if (targetStep === step) return;
    if (targetStep > step) {
      const priorFields = FIELDS.filter((field) => field.step < targetStep).map(
        (field) => field.name,
      );
      const forwardErrors = validateRestrictionForm(values, priorFields);
      if (hasErrors(forwardErrors)) {
        setErrors((prev) => ({ ...prev, ...forwardErrors }));
        const firstBadStep = STEPS.findIndex((_, index) =>
          FIELDS.some((f) => f.step === index && forwardErrors[f.name]),
        );
        if (firstBadStep >= 0 && firstBadStep !== step) {
          setStep(firstBadStep);
        }
        return;
      }
    }
    setStep(targetStep);
  }

  function handleNext() {
    const stepErrors = validateRestrictionForm(
      values,
      stepFields.map((field) => field.name),
    );
    setErrors((prev) => ({ ...prev, ...stepErrors }));
    if (!hasErrors(stepErrors)) {
      setStep((prev) => prev + 1);
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    if (!isLastStep) {
      return;
    }

    const allErrors = validateRestrictionForm(values);
    if (hasErrors(allErrors)) {
      setErrors(allErrors);
      const firstBadStep = STEPS.findIndex((_, index) =>
        FIELDS.some((f) => f.step === index && allErrors[f.name]),
      );
      if (firstBadStep >= 0) setStep(firstBadStep);
      return;
    }

    const token = authService.getStoredToken();
    if (!token) {
      setServerErrors(["Du er ikke logget ind."]);
      return;
    }

    setLoading(true);
    setServerErrors([]);

    try {
      const created = await createRestriction(
        {
          title: values.title.trim(),
          reference: values.reference.trim() || undefined,
          startDate: values.startDate,
          endDate: values.endDate || null,
          situationDescription: values.situationDescription.trim(),
          restrictionDescription: values.restrictionDescription.trim(),
          cancellationDescription: values.cancellationDescription.trim(),
        },
        token,
      );
      navigate(`/restrictions/${created.id}`, {
        state: { notification: "Driftsrestriktion oprettet som kladde." },
      });
    } catch (err) {
      if (err instanceof RestrictionValidationError) {
        setServerErrors(err.messages);
      } else {
        setServerErrors([
          err instanceof Error ? err.message : "Noget gik galt.",
        ]);
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-xl min-w-0 flex-col gap-4 p-4 text-left overflow-x-hidden">
      <header className="flex flex-col gap-1 w-full min-w-0">
        <h1 className="font-ui text-2xl font-bold text-foreground">
          Opret ny Driftsrestriktion
        </h1>
      </header>

      {serverErrors.length > 0 && (
        <Alert variant="destructive" className="w-full min-w-0">
          <AlertTitle>Driftsrestriktionen kunne ikke oprettes</AlertTitle>
          <AlertDescription>
            <ul className="mt-2 list-disc pl-5 font-body text-base">
              {serverErrors.map((message) => (
                <li key={message} className="wrap-break-word">
                  {message}
                </li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      <form
        onSubmit={handleSubmit}
        noValidate
        className="flex flex-col flex-1 w-full min-w-0 max-w-full"
      >
        <Card className="flex flex-col w-full min-w-0 max-w-full border border-border/80 bg-card shadow-sm rounded-2xl overflow-hidden min-h-145 h-[calc(100svh-8rem)] max-h-180">
          <CardHeader className="shrink-0 space-y-3 pb-3 border-b border-border/40 w-full min-w-0 max-w-full overflow-hidden">
            <div className="flex items-center justify-between">
              {step > 0 ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setStep(step - 1)}
                  className="h-7 -ml-2 gap-1 text-muted-foreground hover:text-foreground cursor-pointer"
                  aria-label="Gå til forrige trin"
                >
                  <ArrowLeft className="size-4" />
                  <span className="text-xs font-semibold">Tilbage</span>
                </Button>
              ) : (
                <span className="text-xs font-bold tracking-wider text-muted-foreground uppercase">
                  DRIFTSKONTROL
                </span>
              )}
              <span className="text-xs font-bold text-muted-foreground">
                {step + 1} af {STEPS.length}
              </span>
            </div>

            <div
              role="tablist"
              aria-label="Trin-fremgang"
              className="flex items-center gap-1.5 pt-1"
            >
              {STEPS.map((s, index) => {
                const isActive = index === step;
                const isCompleted = index < step;
                return (
                  <button
                    key={s.title}
                    type="button"
                    role="tab"
                    aria-selected={isActive}
                    aria-label={`Trin ${index + 1}: ${s.title}`}
                    onClick={() => handleTabClick(index)}
                    className="group flex-1 py-2 -my-1 cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1 rounded-full"
                    title={`Trin ${index + 1}: ${s.title}`}
                  >
                    <span
                      className={cn(
                        "block h-2 w-full rounded-full transition-all duration-200",
                        isActive
                          ? "bg-primary"
                          : isCompleted
                            ? "bg-primary/50 group-hover:bg-primary/75"
                            : "bg-muted group-hover:bg-muted-foreground/30",
                      )}
                    />
                  </button>
                );
              })}
            </div>

            <div className="pt-2 text-left">
              <span className="text-xs font-bold tracking-wider text-primary uppercase">
                {STEPS[step].eyebrow}
              </span>
              <CardTitle className="font-serif text-2xl md:text-3xl font-bold text-foreground mt-1">
                {STEPS[step].heading}
              </CardTitle>
              <CardDescription className="text-sm text-muted-foreground mt-1 leading-relaxed">
                {STEPS[step].description}
              </CardDescription>
            </div>
          </CardHeader>

          <CardContent className="flex-1 overflow-y-auto overflow-x-hidden min-w-0 space-y-5 text-left py-4 px-6">
            {stepFields.map((field) => {
              const errorId = `${field.name}-error`;
              const helperId = `${field.name}-helper`;
              const describedBy =
                [
                  errors[field.name] ? errorId : null,
                  "helper" in field && field.helper ? helperId : null,
                ]
                  .filter(Boolean)
                  .join(" ") || undefined;

              return (
                <div key={field.name} className="flex flex-col gap-1.5">
                  <Label
                    htmlFor={field.name}
                    className="text-base font-semibold text-foreground flex items-center justify-between"
                  >
                    <span>
                      {field.uiLabel}
                      {field.uiLabel !== field.label && (
                        <span className="text-xs font-normal text-muted-foreground ml-1.5">
                          ({field.label})
                        </span>
                      )}
                      {field.required && (
                        <span className="text-destructive ml-1">*</span>
                      )}
                      {!field.required && (
                        <span className="text-xs font-normal text-muted-foreground ml-2">
                          (valgfri)
                        </span>
                      )}
                    </span>
                  </Label>

                  {"helper" in field && field.helper && (
                    <p
                      id={helperId}
                      className="text-xs text-muted-foreground -mt-0.5"
                    >
                      {field.helper}
                    </p>
                  )}

                  {field.kind === "textarea" ? (
                    <Textarea
                      id={field.name}
                      value={values[field.name]}
                      onChange={handleChange(field.name)}
                      placeholder={
                        "placeholder" in field ? field.placeholder : undefined
                      }
                      aria-invalid={!!errors[field.name]}
                      aria-describedby={describedBy}
                      className="min-h-28 text-base bg-background resize-y"
                    />
                  ) : (
                    <Input
                      id={field.name}
                      type={field.kind === "date" ? "date" : "text"}
                      value={values[field.name]}
                      onChange={handleChange(field.name)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          handleNext();
                        }
                      }}
                      min={
                        field.name === "endDate"
                          ? values.startDate || undefined
                          : undefined
                      }
                      placeholder={
                        "placeholder" in field ? field.placeholder : undefined
                      }
                      aria-invalid={!!errors[field.name]}
                      aria-describedby={describedBy}
                      className="h-11 text-base bg-background"
                    />
                  )}

                  <div className="flex items-center justify-between text-xs min-h-4">
                    {errors[field.name] ? (
                      <p
                        id={errorId}
                        role="alert"
                        className="font-semibold text-destructive"
                      >
                        {errors[field.name]}
                      </p>
                    ) : (
                      <span />
                    )}
                    {"charLimit" in field && field.charLimit && (
                      <span
                        className={cn(
                          "text-muted-foreground ml-auto",
                          values[field.name].length > field.charLimit &&
                            "text-destructive font-semibold",
                        )}
                      >
                        {values[field.name].length}/{field.charLimit}
                      </span>
                    )}
                  </div>
                </div>
              );
            })}

            {isLastStep && (
              <div className="space-y-3 pt-1 w-full min-w-0 max-w-full">
                <div className="rounded-lg border border-border bg-muted/30 p-3.5 space-y-2 w-full min-w-0 max-w-full overflow-hidden">
                  <div className="flex items-center justify-between text-xs font-semibold text-muted-foreground">
                    <span>Titel og reference</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="xs"
                      onClick={() => setStep(0)}
                      className="h-6 gap-1 text-primary hover:text-primary/80"
                    >
                      <Pencil className="size-3" /> Rediger
                    </Button>
                  </div>
                  <div className="space-y-1 text-sm text-foreground w-full min-w-0 max-w-full">
                    <div>
                      <span className="text-xs text-muted-foreground block font-medium">
                        Titel:
                      </span>
                      <p className="font-medium review-text">
                        {values.title || "Ikke angivet"}
                      </p>
                    </div>
                    <div>
                      <span className="text-xs text-muted-foreground block font-medium">
                        Reference:
                      </span>
                      <p className="review-text">
                        {values.reference || "Ikke angivet"}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="rounded-lg border border-border bg-muted/30 p-3.5 space-y-2 w-full min-w-0 max-w-full overflow-hidden">
                  <div className="flex items-center justify-between text-xs font-semibold text-muted-foreground">
                    <span>Detaljer</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="xs"
                      onClick={() => setStep(1)}
                      className="h-6 gap-1 text-primary hover:text-primary/80"
                    >
                      <Pencil className="size-3" /> Rediger
                    </Button>
                  </div>
                  <div className="space-y-1.5 text-sm text-foreground w-full min-w-0 max-w-full">
                    <div className="min-w-0 max-w-full">
                      <span className="text-xs text-muted-foreground block font-medium">
                        Om situationen:
                      </span>
                      <p className="review-text">
                        {values.situationDescription || "-"}
                      </p>
                    </div>
                    <div className="min-w-0 max-w-full">
                      <span className="text-xs text-muted-foreground block font-medium">
                        Om driftsrestriktionen:
                      </span>
                      <p className="review-text">
                        {values.restrictionDescription || "-"}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="rounded-lg border border-border bg-muted/30 p-3.5 space-y-1 w-full min-w-0 max-w-full overflow-hidden">
                  <div className="flex items-center justify-between text-xs font-semibold text-muted-foreground">
                    <span>Varighed</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="xs"
                      onClick={() => setStep(2)}
                      className="h-6 gap-1 text-primary hover:text-primary/80"
                    >
                      <Pencil className="size-3" /> Rediger
                    </Button>
                  </div>
                  <div className="text-sm text-foreground flex flex-wrap gap-4 w-full min-w-0 max-w-full">
                    <div className="min-w-0">
                      <span className="text-xs text-muted-foreground block font-medium">
                        Startdato:
                      </span>
                      <span className="break-all">
                        {values.startDate || "Ikke valgt"}
                      </span>
                    </div>
                    <div className="min-w-0">
                      <span className="text-xs text-muted-foreground block font-medium">
                        Slutdato:
                      </span>
                      <span className="break-all">
                        {values.endDate || "Uoplyst slutdato"}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="rounded-lg border border-border bg-muted/30 p-3.5 space-y-1 w-full min-w-0 max-w-full overflow-hidden">
                  <div className="flex items-center justify-between text-xs font-semibold text-muted-foreground">
                    <span>Om ophævelse</span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="xs"
                      onClick={() => setStep(3)}
                      className="h-6 gap-1 text-primary hover:text-primary/80"
                    >
                      <Pencil className="size-3" /> Rediger
                    </Button>
                  </div>
                  <p className="text-sm font-medium text-foreground review-text">
                    {values.cancellationDescription || "-"}
                  </p>
                </div>
              </div>
            )}
          </CardContent>

          <CardFooter className="shrink-0 mt-auto flex items-center justify-between gap-3 pt-3 pb-4 px-6 border-t border-border/50 bg-card">
            {step > 0 ? (
              <Button
                key="back-step-btn"
                type="button"
                variant="outline"
                onClick={() => setStep(step - 1)}
                disabled={loading}
                className="gap-1.5 font-medium"
              >
                <ArrowLeft className="size-4" /> Tilbage
              </Button>
            ) : (
              <span />
            )}

            {isLastStep ? (
              <Button
                key="submit-step-btn"
                type="submit"
                disabled={loading}
                className="gap-2 bg-primary hover:bg-primary/90 font-semibold px-5"
              >
                {loading ? "Opretter..." : "Opret kladde"}
                {!loading && <Check className="size-4" />}
              </Button>
            ) : (
              <Button
                key="next-step-btn"
                type="button"
                onClick={handleNext}
                className="gap-2 bg-primary hover:bg-primary/90 font-semibold px-5"
              >
                Fortsæt <ArrowRight className="size-4" />
              </Button>
            )}
          </CardFooter>
        </Card>
      </form>
    </div>
  );
}
