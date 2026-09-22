// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
import { sendExampleRequest } from "../services/exampleService";

export function ExampleComponent() {
  return (
    <button onClick={() => sendExampleRequest()}>Send example request</button>
  );
}
