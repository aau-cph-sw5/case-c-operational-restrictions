// COMPONENT:
// Reusable card displaying signature completeness for one Operational Restriction.
// Expands to show the signature status of each CCR operator.

import { useState } from "react";
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  OctagonAlert,
} from "lucide-react";
import { Card, CardContent } from "./ui/card";

import type {
  OperatorSignatureStatusDto,
  RestrictionInForceDto,
} from "../types/signatureCompleteness";

interface SignatureRestrictionCardProps {
  restriction: RestrictionInForceDto;
  operators: OperatorSignatureStatusDto[];
}

export function SignatureRestrictionCard({
  restriction,
  operators,
}: SignatureRestrictionCardProps) {
  const [expanded, setExpanded] = useState(false);

  const employeeStatuses = operators.map((operator) => {
    const signature = operator.cells.find(
      (cell) => cell.restrictionId === restriction.restrictionId,
    );

    return {
      id: operator.operatorId,
      name: operator.name,
      signed: signature?.isSigned ?? false,
      signedAt: signature?.signedAt ?? null,
    };
  });

  const signedCount = employeeStatuses.filter(
    (employee) => employee.signed,
  ).length;

  const missingCount = employeeStatuses.length - signedCount;

  const hasEmployees = employeeStatuses.length > 0;

  return (
    <Card className="gap-0 border border-[#E0F3FF] bg-white py-0 ring-0">
      <button
        type="button"
        aria-expanded={expanded}
        onClick={() => setExpanded(!expanded)}
        className="w-full cursor-pointer p-4 text-left"
      >
        <div className="flex items-center justify-between gap-3">
          <span className="font-['Instrument_Sans',sans-serif] text-base font-bold">
            {restriction.reference || restriction.restrictionId}
          </span>

          <div className="flex items-center gap-2">
            <span
              className={`rounded-md px-2 py-1 font-['Inter',sans-serif] text-sm ${
                !hasEmployees
                  ? "bg-gray-100 text-[#6B6B6B]"
                  : missingCount > 0
                    ? "bg-[#FEF3C7] text-[#EF7D00]"
                    : "bg-[#DAFBEA] text-[#1E745B]"
              }`}
            >
              {!hasEmployees
                ? "No employee data"
                : missingCount > 0
                  ? `${missingCount} missing`
                  : "Complete"}
            </span>

            {expanded ? (
              <ChevronUp className="size-5" aria-hidden />
            ) : (
              <ChevronDown className="size-5" aria-hidden />
            )}
          </div>
        </div>

        <p className="mt-2 font-['Inria_Serif',serif] text-[20px]">
          {restriction.title}
        </p>

        <p className="mt-2 font-['Inter',sans-serif] text-sm text-[#6B6B6B]">
          {signedCount} of {employeeStatuses.length} signed
        </p>
      </button>

      {expanded && (
        <CardContent className="border-t border-[#E0F3FF] px-4 py-4">
          <h3 className="font-['Inria_Serif',serif] text-[22px] font-bold">
            Employee signatures
          </h3>

          {employeeStatuses.length === 0 ? (
            <p className="font-['Inria_Serif',serif] text-[20px] text-[#6B6B6B]">
              No operators included in this report.
            </p>
          ) : (
            <ul className="space-y-3">
              {employeeStatuses.map((employee) => (
                <li
                  key={employee.id}
                  className="flex items-center justify-between gap-3"
                >
                  <span className="font-['Inria_Serif',serif] text-[20px]">
                    {employee.name}
                  </span>

                  {employee.signed ? (
                    <span className="flex items-center gap-2 text-[#1E745B]">
                      <CheckCircle2 className="size-5 shrink-0" aria-hidden />
                      Signed
                    </span>
                  ) : (
                    <span className="flex items-center gap-2 text-[#EF7D00]">
                      <OctagonAlert className="size-5 shrink-0" aria-hidden />
                      Missing
                    </span>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      )}
    </Card>
  );
}
