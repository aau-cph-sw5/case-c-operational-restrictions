// PAGE:
// Step-by-step form for an Originator to create an Operational Restriction draft.
import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useNavigate } from "react-router-dom";
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

  const fieldStyle = {
    display: "flex",
    flexDirection: "column" as const,
    gap: "0.75rem",
  };

  return (
    <div className="container">
      <h1>New Operational Restriction</h1>
      <p>
        Step {step + 1} of {STEPS.length}: {STEPS[step].title}
      </p>

      {serverErrors.length > 0 && (
        <div role="alert" className="error">
          <ul>
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate className="backend-card">
        <div style={fieldStyle}>
          {step === 0 && (
            <>
              <label htmlFor="title">
                Title *
                <input
                  id="title"
                  value={values.title}
                  onChange={handleChange("title")}
                />
              </label>
              {errors.title && <p className="error">{errors.title}</p>}

              <label htmlFor="description">
                Description *
                <textarea
                  id="description"
                  value={values.description}
                  onChange={handleChange("description")}
                />
              </label>
              {errors.description && (
                <p className="error">{errors.description}</p>
              )}
            </>
          )}

          {step === 1 && (
            <>
              <label htmlFor="startDate">
                Start date *
                <input
                  id="startDate"
                  type="date"
                  value={values.startDate}
                  onChange={handleChange("startDate")}
                />
              </label>
              {errors.startDate && <p className="error">{errors.startDate}</p>}

              <label htmlFor="endDate">
                End date *
                <input
                  id="endDate"
                  type="date"
                  value={values.endDate}
                  onChange={handleChange("endDate")}
                />
              </label>
              {errors.endDate && <p className="error">{errors.endDate}</p>}
            </>
          )}

          {step === 2 && (
            <>
              <label htmlFor="location">
                Location *
                <input
                  id="location"
                  value={values.location}
                  onChange={handleChange("location")}
                />
              </label>
              {errors.location && <p className="error">{errors.location}</p>}

              <label htmlFor="notes">
                Notes
                <textarea
                  id="notes"
                  value={values.notes}
                  onChange={handleChange("notes")}
                />
              </label>
            </>
          )}

          <div style={{ display: "flex", gap: "0.75rem" }}>
            {step > 0 && (
              <button
                type="button"
                onClick={() => setStep(step - 1)}
                disabled={loading}
              >
                Back
              </button>
            )}
            {isLastStep ? (
              <button type="submit" disabled={loading}>
                {loading ? "Creating..." : "Create draft"}
              </button>
            ) : (
              <button type="button" onClick={handleNext}>
                Next
              </button>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}