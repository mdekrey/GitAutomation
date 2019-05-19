import React from "react";
import "./Card.css";

const CardContext = React.createContext((jsx: React.ReactNode) => jsx);

export function Card({
  children,
  className = "",
}: {
  children?: React.ReactNode;
  className?: string;
}) {
  const [actionBar, setActionBar] = React.useState<React.ReactNode>(null);
  return (
    <CardContext.Provider
      value={jsx => {
        setActionBar(jsx);
        return null;
      }}>
      <div className={`Card_card ${className}`}>
        <div className={`Card_contents`}>{children}</div>
        {actionBar}
      </div>
    </CardContext.Provider>
  );
}

export function ActionBar({ children }: { children?: React.ReactNode }) {
  const [output, setOutput] = React.useState(children);
  const setActionBar = React.useContext(CardContext);
  React.useEffect(() => {
    setOutput(
      setActionBar(
        <div className="Card_action-row">
          <div className="Card_filler" />
          {children}
        </div>
      )
    );
    return () => {
      setActionBar(null);
    };
  }, [children, setActionBar]);
  return <>{output}</>;
}
