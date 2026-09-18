import Link from "next/link";

export default function Denied() {
  return <main className="page"><section className="card result"><h1>Application denied</h1><p>We’re unable to approve this application.</p><Link href="/">Submit another application</Link></section></main>;
}
