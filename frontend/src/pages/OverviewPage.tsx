import { Link } from "react-router-dom";

export function OverviewPage() {
  return (
    <div className="container">
      <h1>Overview</h1>
      <p>This is the overview page of the application.</p>
      
      <p>
        <Link to="/home">Back to Home</Link>
      </p>
    </div>
  );
}