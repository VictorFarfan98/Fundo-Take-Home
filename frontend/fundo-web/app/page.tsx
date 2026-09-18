"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

type Fields = { firstName: string; lastName: string; address: string; state: string; companyName: string; requestedAmount: string; ssn: string };
type Errors = Partial<Record<keyof Fields, string>> & { form?: string };

const initialFields: Fields = { firstName: "", lastName: "", address: "", state: "", companyName: "", requestedAmount: "", ssn: "" };

function validate(values: Fields): Errors {
  const errors: Errors = {};
  for (const [name, value] of Object.entries(values) as [keyof Fields, string][])
    if (!value.trim()) errors[name] = "This field is required.";
  if (values.state && !/^[a-z]{2}$/i.test(values.state.trim())) errors.state = "Use a two-letter state code.";
  if (values.requestedAmount && Number(values.requestedAmount) <= 0) errors.requestedAmount = "Enter an amount greater than zero.";
  if (values.ssn && values.ssn.replace(/\D/g, "").length !== 9) errors.ssn = "Enter an SSN with nine digits.";
  return errors;
}

export default function ApplicationForm() {
  const router = useRouter();
  const [fields, setFields] = useState(initialFields);
  const [errors, setErrors] = useState<Errors>({});
  const [submitting, setSubmitting] = useState(false);

  function update(name: keyof Fields, value: string) {
    setFields(current => ({ ...current, [name]: value }));
    setErrors(current => ({ ...current, [name]: undefined, form: undefined }));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting) return;
    const validation = validate(fields);
    if (Object.keys(validation).length) return setErrors(validation);
    setSubmitting(true);
    try {
      const response = await fetch("/api/applications", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...fields, requestedAmount: Number(fields.requestedAmount) })
      });
      const body = await response.json().catch(() => null);
      if (response.status === 422 && body?.decision === "denied") return router.push("/denied");
      if (response.ok && body?.decision === "approved" && body.applicationId)
        return router.push(`/approved?applicationId=${encodeURIComponent(body.applicationId)}`);
      setErrors({ form: response.status === 400 ? "Please correct the highlighted information and try again." : "We could not submit your application. Please try again." });
    } catch {
      setErrors({ form: "We could not reach the application service. Please try again." });
    } finally {
      setSubmitting(false);
    }
  }

  return <main className="page"><section className="card"><h1>Apply for a Fundo loan</h1><p className="intro">Tell us a little about yourself to get a decision.</p>
    <form onSubmit={submit} noValidate aria-busy={submitting}>
      {errors.form && <p className="form-error" role="alert">{errors.form}</p>}
      <div className="fields">
        <Field label="First name" name="firstName" value={fields.firstName} error={errors.firstName} disabled={submitting} onChange={update} />
        <Field label="Last name" name="lastName" value={fields.lastName} error={errors.lastName} disabled={submitting} onChange={update} />
        <Field label="Address" name="address" value={fields.address} error={errors.address} disabled={submitting} onChange={update} className="wide" />
        <Field label="State" name="state" value={fields.state} error={errors.state} disabled={submitting} onChange={update} maxLength={2} />
        <Field label="Company name" name="companyName" value={fields.companyName} error={errors.companyName} disabled={submitting} onChange={update} />
        <Field label="Requested amount" name="requestedAmount" value={fields.requestedAmount} error={errors.requestedAmount} disabled={submitting} onChange={update} type="number" min="0.01" step="0.01" />
        <Field label="SSN" name="ssn" value={fields.ssn} error={errors.ssn} disabled={submitting} onChange={update} inputMode="numeric" autoComplete="off" />
      </div>
      <button disabled={submitting} type="submit">{submitting ? "Submitting…" : "Submit application"}</button>
    </form>
  </section></main>;
}

function Field({ label, name, value, error, disabled, onChange, className = "", ...input }: { label: string; name: keyof Fields; value: string; error?: string; disabled: boolean; onChange: (name: keyof Fields, value: string) => void; className?: string } & Omit<React.InputHTMLAttributes<HTMLInputElement>, "onChange" | "name" | "value" | "disabled">) {
  const id = `application-${name}`;
  return <label className={`field ${className}`} htmlFor={id}>{label}<input {...input} id={id} name={name} value={value} disabled={disabled} onChange={event => onChange(name, event.target.value)} aria-invalid={Boolean(error)} aria-describedby={error ? `${id}-error` : undefined} />{error && <span id={`${id}-error`} className="field-error">{error}</span>}</label>;
}
