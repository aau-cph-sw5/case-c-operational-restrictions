// PAGE:
// Trin-for-trin formular til at oprette en kladde af en driftsrestriktion.
import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "../components/ui/button";
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

const controlClass =
  "h-11 border-pattens-blue bg-white font-body text-lg md:text-lg";
const textareaClass =
  "min-h-32 border-pattens-blue bg-white font-body text-lg md:text-lg";

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

  function handleNext() {
    const stepErrors = validateRestrictionForm(
      values,
      stepFields.map((field) => field.name),
    );
    setErrors(stepErrors);
    if (!hasErrors(stepErrors)) setStep(step + 1);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    // Enter på et tidligere trin virker som "Næste".
    if (!isLastStep) {
      handleNext();
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
          startDate: values.startDate,
          endDate: values.endDate || null,
          reference: values.reference.trim() || undefined,
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
    <div className="mx-auto flex max-w-xl flex-col gap-4 p-4 text-left">
      <header className="flex flex-col gap-1">
        <h1 className="font-ui text-2xl font-bold text-black">
          Ny driftsrestriktion
        </h1>
        <p className="font-ui text-base font-bold text-anvil-grey">
          Trin {step + 1} af {STEPS.length}
        </p>
      </header>

      {serverErrors.length > 0 && (
        <div
          role="alert"
          className="rounded-lg border border-gold-drop bg-beeswax p-4"
        >
          <p className="font-ui text-base text-gold-drop">
            Driftsrestriktionen kunne ikke oprettes
          </p>
          <ul className="mt-2 list-disc pl-5 font-body text-lg text-black">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        noValidate
        className="flex flex-col gap-5 rounded-xl border border-pattens-blue bg-zumthor p-6"
      >
        <div className="flex gap-2" aria-hidden="true">
          {STEPS.map((s, index) => (
            <div
              key={s.title}
              className={`h-1.5 flex-1 rounded-full ${
                index <= step ? "bg-mirror-blue" : "bg-pattens-blue"
              }`}
            />
          ))}
        </div>

        <h2 className="font-ui text-base font-bold text-mirror-blue">
          {STEPS[step].title}
        </h2>

        {stepFields.map((field) => (
          <div key={field.name} className="flex flex-col gap-2">
            <Label
              htmlFor={field.name}
              className="font-body text-xl text-black"
            >
              {field.label}
              {field.required ? " *" : " (valgfri)"}
            </Label>

            {field.kind === "textarea" ? (
              <Textarea
                id={field.name}
                value={values[field.name]}
                onChange={handleChange(field.name)}
                aria-invalid={!!errors[field.name]}
                className={textareaClass}
              />
            ) : (
              <Input
                id={field.name}
                type={field.kind === "date" ? "date" : "text"}
                value={values[field.name]}
                onChange={handleChange(field.name)}
                aria-invalid={!!errors[field.name]}
                className={controlClass}
              />
            )}

            {errors[field.name] && (
              <p className="font-ui text-sm font-bold text-gold-drop">
                {errors[field.name]}
              </p>
            )}
          </div>
        ))}

        <div className="flex justify-between pt-2">
          {step > 0 ? (
            <Button
              type="button"
              variant="outline"
              className="font-ui font-bold"
              onClick={() => setStep(step - 1)}
              disabled={loading}
            >
              Tilbage
            </Button>
          ) : (
            <span />
          )}
          {isLastStep ? (
            <Button
              type="submit"
              className="font-ui font-bold"
              disabled={loading}
            >
              {loading ? "Opretter..." : "Opret kladde"}
            </Button>
          ) : (
            <Button
              type="button"
              className="font-ui font-bold"
              onClick={handleNext}
            >
              Næste
            </Button>
          )}
        </div>
      </form>
    </div>
  );
}
