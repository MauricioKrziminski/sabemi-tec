import { Suspense } from "react";
import { Dashboard } from "@/components/dashboard/dashboard";

export default function Home() {
  return (
    <main>
      <Suspense>
        <Dashboard />
      </Suspense>
    </main>
  );
}
