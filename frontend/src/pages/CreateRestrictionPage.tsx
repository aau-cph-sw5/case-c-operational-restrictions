// PAGE:
// Step-by-step form for an Originator to create an Operational Restriction draft.
import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";
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
import {
  EMPTY_VALUES,
  hasErrors,
  validateRestrictionForm,
} from "../lib/restrictionValidation";
import type {
  FormErrors,
  RestrictionField,
  RestrictionFormValues,
} from "../lib/restrictionValidation";

const STEPS: { title: string; fields: RestrictionField[] }[] = [
  { title: "Basics", fields: ["title", "description"] },
  { title: "Period", fields: ["startDate", "endDate"] },
  { title: "Location", fields: ["location", "notes"] },
];

function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="text-sm text-destructive">{message}</p>;
}

export function CreateRestrictionPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [values, setValues] = useState<RestrictionFormValues>(EMPTY_VALUES);
  const [errors, setErrors] = useState<FormErrors>({});
  const [serverErrors, setServerErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const isLastStep = step === STEPS.length - 1;

  function handleChange(field: RestrictionField) {
    return (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setValues((prev) => ({ ...prev, [field]: e.target.value }));
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    };
  }

  function handleNext() {
    const stepErrors = validateRestrictionForm(values, STEPS[step].fields);
    setErrors(stepErrors);
    if (!hasErrors(stepErrors)) setStep(step + 1);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    // Pressing Enter on an earlier step behaves like "Next".
    if (!isLastStep) {
      handleNext();
      return;
    }

    const allErrors = validateRestrictionForm(values);
    if (hasErrors(allErrors)) {
      setErrors(allErrors);
      const firstBadStep = STEPS.findIndex((s) =>
        s.fields.some((f) => allErrors[f]),
      );
      if (firstBadStep >= 0) setStep(firstBadStep);
      return;
    }

    const token = authService.getStoredToken();
    if (!token) {
      setServerErrors(["You are not logged in."]);
      return;
    }

    setLoading(true);
    setServerErrors([]);

    try {
      const created = await createRestriction(
        {
          title: values.title.trim(),
          description: values.description.trim(),
          location: values.location.trim(),
          startDate: values.startDate,
          endDate: values.endDate,
          notes: values.notes.trim() || undefined,
        },
        token,
      );
      navigate(`/restrictions/${created.id}`, {
        state: { notification: "Restriction draft created." },
      });
    } catch (err) {
      if (err instanceof RestrictionValidationError) {
        setServerErrors(err.messages);
      } else {
        setServerErrors([
          err instanceof Error ? err.message : "Something went wrong.",
        ]);
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto flex max-w-xl flex-col gap-4 p-4">
      {serverErrors.length > 0 && (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Could not create the restriction</AlertTitle>
          <AlertDescription>
            <ul className="list-disc pl-4">
              {serverErrors.map((message) => (
                <li key={message}>{message}</li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>New Operational Restriction</CardTitle>
          <CardDescription>
            Step {step + 1} of {STEPS.length}: {STEPS[step].title}
          </CardDescription>
          <div className="flex gap-2 pt-2" aria-hidden="true">
            {STEPS.map((s, index) => (
              <div
                key={s.title}
                className={`h-1.5 flex-1 rounded-full ${
                  index <= step ? "bg-primary" : "bg-muted"
                }`}
              />
            ))}
          </div>
        </CardHeader>

        <form onSubmit={handleSubmit} noValidate>
          <CardContent className="flex flex-col gap-4">
            {step === 0 && (
              <>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="title">Title *</Label>
                  <Input
                    id="title"
                    value={values.title}
                    onChange={handleChange("title")}
                    aria-invalid={!!errors.title}
                  />
                  <FieldError message={errors.title} />
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="description">Description *</Label>
                  <Textarea
                    id="description"
                    value={values.description}
                    onChange={handleChange("description")}
                    aria-invalid={!!errors.description}
                  />
                  <FieldError message={errors.description} />
                </div>
              </>
            )}

            {step === 1 && (
              <>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="startDate">Start date *</Label>
                  <Input
                    id="startDate"
                    type="date"
                    value={values.startDate}
                    onChange={handleChange("startDate")}
                    aria-invalid={!!errors.startDate}
                  />
                  <FieldError message={errors.startDate} />
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="endDate">End date *</Label>
                  <Input
                    id="endDate"
                    type="date"
                    value={values.endDate}
                    onChange={handleChange("endDate")}
                    aria-invalid={!!errors.endDate}
                  />
                  <FieldError message={errors.endDate} />
                </div>
              </>
            )}

            {step === 2 && (
              <>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="location">Location *</Label>
                  <Input
                    id="location"
                    value={values.location}
                    onChange={handleChange("location")}
                    aria-invalid={!!errors.location}
                  />
                  <FieldError message={errors.location} />
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="notes">Notes</Label>
                  <Textarea
                    id="notes"
                    value={values.notes}
                    onChange={handleChange("notes")}
                  />
                </div>
              </>
            )}
          </CardContent>

          <CardFooter className="justify-between pt-6">
            {step > 0 ? (
              <Button
                type="button"
                variant="outline"
                onClick={() => setStep(step - 1)}
                disabled={loading}
              >
                Back
              </Button>
            ) : (
              <span />
            )}
            {isLastStep ? (
              <Button type="submit" disabled={loading}>
                {loading ? "Creating..." : "Create draft"}
              </Button>
            ) : (
              <Button type="button" onClick={handleNext}>
                Next
              </Button>
            )}
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
