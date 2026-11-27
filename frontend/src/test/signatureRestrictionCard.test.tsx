import { render, screen, fireEvent } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { SignatureRestrictionCard } from "../components/SignatureRestrictionCard";
import type {
  RestrictionInForceDto,
  OperatorSignatureStatusDto,
} from "../types/signatureCompleteness";

const restriction: RestrictionInForceDto = {
  restrictionId: "restriction-1",
  title: "Speed restriction at Kongens Nytorv",
  reference: "REF-2026-007",
};

const operators: OperatorSignatureStatusDto[] = [
  {
    operatorId: "user-1",
    name: "Operator One",
    cells: [
      {
        restrictionId: "restriction-1",
        isSigned: false,
        signedAt: null,
      },
    ],
  },
  {
    operatorId: "user-2",
    name: "Operator Two",
    cells: [
      {
        restrictionId: "restriction-1",
        isSigned: false,
        signedAt: null,
      },
    ],
  },
];

describe("SignatureRestrictionCard", () => {
  it("shows the number of missing signatures", () => {
    render(
      <SignatureRestrictionCard
        restriction={restriction}
        operators={operators}
      />,
    );

    expect(screen.getByText("2 missing")).toBeInTheDocument();
    expect(screen.getByText("0 of 2 signed")).toBeInTheDocument();
  });

  it("shows Complete when all operators have signed", () => {
    const signedOperators = operators.map((operator) => ({
      ...operator,
      cells: operator.cells.map((cell) => ({
        ...cell,
        isSigned: true,
        signedAt: "2026-11-18T10:00:00Z",
      })),
    }));

    render(
      <SignatureRestrictionCard
        restriction={restriction}
        operators={signedOperators}
      />,
    );

    expect(screen.getByText("Complete")).toBeInTheDocument();
    expect(screen.getByText("2 of 2 signed")).toBeInTheDocument();
  });

  it("shows No employee data when there are no operators", () => {
    render(
      <SignatureRestrictionCard restriction={restriction} operators={[]} />,
    );

    expect(screen.getByText("No employee data")).toBeInTheDocument();
  });

  it("shows employee signatures when the card is expanded", () => {
    render(
      <SignatureRestrictionCard
        restriction={restriction}
        operators={operators}
      />,
    );

    const button = screen.getByRole("button");

    expect(button).toHaveAttribute("aria-expanded", "false");

    fireEvent.click(button);

    expect(button).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Employee signatures")).toBeInTheDocument();
    expect(screen.getByText("Operator One")).toBeInTheDocument();
    expect(screen.getByText("Operator Two")).toBeInTheDocument();
  });
});
