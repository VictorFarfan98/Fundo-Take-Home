import Link from "next/link";

export default async function Approved({ searchParams }: { searchParams: Promise<{ applicationId?: string }> }) {
  const { applicationId } = await searchParams;
  return <Result title="Application approved" message="Your loan application has been approved." detail={applicationId ? `Application ID: ${applicationId}` : undefined} />;
}

function Result({ title, message, detail }: { title: string; message: string; detail?: string }) {
  return <main className="page"><section className="card result"><h1>{title}</h1><p>{message}</p>{detail && <p className="detail">{detail}</p>}<Link href="/">Submit another application</Link></section></main>;
}
