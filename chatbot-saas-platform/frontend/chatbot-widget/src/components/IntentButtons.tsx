import React from "react";
import { useDispatch, useSelector } from "react-redux";
import { RootState } from "../store/store";
import { sendMessage } from "../store/slices/chatSlice";

export const IntentButtons: React.FC = () => {
  const dispatch = useDispatch();
  const { widget } = useSelector((state: RootState) => state.config);
  const predefinedIntents = widget.predefinedIntents || [];

  const handleIntentClick = (intent: {
    id: string;
    label: string;
    message: string;
    category: string;
    isActive: boolean;
  }) => {
    dispatch(sendMessage({
      content: intent.message,
      type: 'text'
    }));
  };

  if (predefinedIntents.length === 0) return null;

  const activeIntents = predefinedIntents.filter(intent => intent.isActive);
  
  if (activeIntents.length === 0) return null;

  return (
    <div className="intent-buttons">
      <div className="intent-buttons-title">Quick Start:</div>
      <div className="intent-buttons-grid">
        {activeIntents.map((intent) => (
          <button
            key={intent.id}
            className="intent-button"
            onClick={() => handleIntentClick(intent)}
          >
            {intent.label}
          </button>
        ))}
      </div>
    </div>
  );
};
