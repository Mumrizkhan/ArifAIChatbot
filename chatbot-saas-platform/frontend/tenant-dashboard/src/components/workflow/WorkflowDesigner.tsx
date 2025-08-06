import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { WorkflowService, Workflow, WorkflowDefinition } from '../../services/workflowService';

interface WorkflowDesignerProps {
  workflow?: Workflow | null;
  onSave: (definition: WorkflowDefinition) => void;
  onCancel: () => void;
}

export const WorkflowDesigner: React.FC<WorkflowDesignerProps> = ({
  workflow,
  onSave,
  onCancel
}) => {
  const { t } = useTranslation();
  const [definition, setDefinition] = useState<WorkflowDefinition>({
    steps: [],
    connections: [],
    layout: { width: 800, height: 600, zoom: 1, viewportPosition: { x: 0, y: 0 } }
  });
  const [isValidating, setIsValidating] = useState(false);
  const [validationMessage, setValidationMessage] = useState<string>('');

  useEffect(() => {
    if (workflow?.definition) {
      setDefinition(workflow.definition);
    }
  }, [workflow]);

  const handleSave = async () => {
    setIsValidating(true);
    setValidationMessage('');
    
    try {
      const validationResponse = await WorkflowService.validateWorkflowDefinition(definition);
      if (validationResponse.data.isValid) {
        onSave(validationResponse.data.definition);
      } else {
        setValidationMessage(validationResponse.data.message || 'Workflow validation failed');
      }
    } catch (error) {
      setValidationMessage('Failed to validate workflow definition');
      console.error('Validation error:', error);
    } finally {
      setIsValidating(false);
    }
  };

  const addStep = () => {
    const newStep = {
      id: `step_${Date.now()}`,
      type: 'action',
      name: 'New Step',
      position: { x: 100, y: 100 },
      configuration: {}
    };
    
    setDefinition(prev => ({
      ...prev,
      steps: [...prev.steps, newStep as any]
    }));
  };

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-4 mx-auto p-5 border w-full max-w-6xl shadow-lg rounded-md bg-white dark:bg-gray-800">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-medium text-gray-900 dark:text-white">
            {workflow ? t('workflows.editWorkflow') : t('workflows.designWorkflow')}
          </h3>
          <div className="space-x-2">
            <button
              onClick={addStep}
              className="px-3 py-1 text-sm font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-md hover:bg-blue-100"
            >
              Add Step
            </button>
            <button
              onClick={handleSave}
              disabled={isValidating}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
            >
              {isValidating ? 'Validating...' : t('common.save')}
            </button>
            <button
              onClick={onCancel}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 dark:bg-gray-600 dark:text-gray-300 dark:border-gray-500 dark:hover:bg-gray-700"
            >
              {t('common.cancel')}
            </button>
          </div>
        </div>

        {validationMessage && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
            <p className="text-red-800 text-sm">{validationMessage}</p>
          </div>
        )}

        <div className="h-96 border border-gray-300 rounded-md bg-gray-50 dark:bg-gray-700 relative overflow-hidden">
          <div className="absolute inset-0 flex items-center justify-center">
            {definition.steps.length === 0 ? (
              <div className="text-center">
                <p className="text-gray-500 dark:text-gray-400 mb-4">
                  {t('workflows.designerPlaceholder')}
                </p>
                <button
                  onClick={addStep}
                  className="px-4 py-2 text-sm font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-md hover:bg-blue-100"
                >
                  Add First Step
                </button>
              </div>
            ) : (
              <div className="w-full h-full p-4">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {definition.steps.map((step) => (
                    <div
                      key={step.id}
                      className="p-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-md shadow-sm"
                    >
                      <div className="flex justify-between items-start mb-2">
                        <h4 className="text-sm font-medium text-gray-900 dark:text-white">
                          {step.name}
                        </h4>
                        <span className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded">
                          {step.type}
                        </span>
                      </div>
                      <p className="text-xs text-gray-600 dark:text-gray-400">
                        {(step as any).description || 'No description'}
                      </p>
                      <div className="mt-2 flex justify-end">
                        <button
                          onClick={() => {
                            setDefinition(prev => ({
                              ...prev,
                              steps: prev.steps.filter(s => s.id !== step.id)
                            }));
                          }}
                          className="text-xs text-red-600 hover:text-red-800"
                        >
                          Remove
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="mt-4 text-sm text-gray-600 dark:text-gray-400">
          <p>Steps: {definition.steps.length} | Connections: {definition.connections.length}</p>
          <p className="text-xs mt-1">
            This is a basic workflow designer. Steps can be added and removed. 
            Advanced visual editing features can be implemented in future iterations.
          </p>
        </div>
      </div>
    </div>
  );
};
