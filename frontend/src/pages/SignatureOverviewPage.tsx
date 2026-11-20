// PAGE:
// Route-level screen component rendered by React Router for a specific URL.
// Displays signature completeness across active Operational Restrictions.

import { useEffect, useState } from "react";
import { Clock3, LoaderCircle, OctagonAlert, Search } from "lucide-react";

import { SignatureRestrictionCard } from "../components/SignatureRestrictionCard";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { getSignatureCompleteness } from "../services/signatureCompletenessApi";

import type { SignatureCompletenessReportDto } from "../types/signatureCompleteness";

type SignatureFilter = "all" | "missing" | "complete";

export function SignatureOverviewPage() {
  const [report, setReport] = useState<SignatureCompletenessReportDto | null>(
    null,
  );

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<SignatureFilter>("all");

  useEffect(() => {
    let cancelled = false;

    async function loadReport() {
      try {
        const data = await getSignatureCompleteness();

        if (!cancelled) {
          setReport(data);
        }
      } catch {
        if (!cancelled) {
          setError("Could not load signature overview.");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadReport();

    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <main className="mx-auto max-w-md p-4">
        <p role="status" className="flex items-center gap-2">
          <LoaderCircle className="size-5 animate-spin" aria-hidden />
          Loading signature overview...
        </p>
      </main>
    );
  }

  if (error || !report) {
    return (
      <main className="mx-auto max-w-md p-4">
        <p role="alert" className="flex items-center gap-2 text-[#EF7D00]">
          <OctagonAlert className="size-5" aria-hidden />
          {error ?? "Signature overview unavailable."}
        </p>
      </main>
    );
  }

  const getMissingCount = (restrictionId: string) =>
    report.operators.filter((operator) => {
      const cell = operator.cells.find(
        (cell) => cell.restrictionId === restrictionId,
      );

      return cell?.isSigned !== true;
    }).length;

  const filteredRestrictions = report.restrictions.filter((restriction) => {
    const query = search.toLowerCase().trim();

    const matchesSearch =
      restriction.title.toLowerCase().includes(query) ||
      restriction.restrictionId.toLowerCase().includes(query) ||
      (restriction.reference ?? "").toLowerCase().includes(query);

    const missingCount = getMissingCount(restriction.restrictionId);

    const matchesFilter =
      filter === "all" ||
      (filter === "missing" && missingCount > 0) ||
      (filter === "complete" &&
        missingCount === 0 &&
        report.operators.length > 0);

    return matchesSearch && matchesFilter;
  });

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-5xl flex-col gap-6 bg-white px-4 py-6 sm:px-6 lg:px-8">
      <header className="space-y-1">
        <h1 className="font-['Instrument_Sans',sans-serif] text-2xl font-bold text-black">
          Signature Overview
        </h1>

        <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]">
          CCR employee signature status
        </p>
        <p className="flex items-center justify-center gap-1 font-['Inter',sans-serif] text-xs text-[#6B6B6B]">
          <Clock3 className="size-3" aria-hidden />
          Last updated: {new Date(report.computedAt).toLocaleString()}
        </p>
      </header>

      <div className="grid grid-cols-2 gap-3">
        <Card className="gap-2 border border-[#E0F3FF] bg-[#EFF6FF] py-3 ring-0">
          <CardHeader>
            <CardTitle>Active restrictions</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{report.restrictions.length}</p>
          </CardContent>
        </Card>

        <Card className="gap-2 border border-[#E0F3FF] bg-[#EFF6FF] py-3 ring-0">
          <CardHeader>
            <CardTitle>Missing signatures</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-[#EF7D00]">
              {report.outstandingSignatureCount}
            </p>
          </CardContent>
        </Card>
      </div>

      <section aria-label="Filter restrictions" className="space-y-3">
        <div className="relative">
          <Search
            className="absolute top-1/2 left-3 size-5 -translate-y-1/2 text-[#6B6B6B]"
            aria-hidden
          />

          <Input
            aria-label="Search restrictions"
            placeholder="Search restrictions..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="pl-10"
          />
        </div>

        <div className="flex gap-2">
          {(["all", "missing", "complete"] as const).map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setFilter(option)}
              aria-pressed={filter === option}
              className={`rounded-lg px-3 py-2 font-['Inter',sans-serif] text-sm font-medium ${
                filter === option
                  ? "bg-[#2563EB] text-white"
                  : "bg-[#EFF6FF] text-[#2563EB]"
              }`}
            >
              {option === "missing"
                ? "Missing"
                : option === "all"
                  ? "All"
                  : "Complete"}
            </button>
          ))}
        </div>
      </section>

      <section aria-label="Restrictions" className="space-y-3">
        <h2 className="font-['Inria_Serif',serif] text-[22px] font-bold">
          Restrictions ({filteredRestrictions.length})
        </h2>

        {filteredRestrictions.length === 0 ? (
          <p className="text-[#6B6B6B]">
            No restrictions match your search or filter.
          </p>
        ) : (
          <div className="grid w-full grid-cols-1 items-start gap-4 md:grid-cols-2">
            {filteredRestrictions.map((restriction) => (
              <SignatureRestrictionCard
                key={restriction.restrictionId}
                restriction={restriction}
                operators={report.operators}
              />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
