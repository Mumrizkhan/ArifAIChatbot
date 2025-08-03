import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector, useAppDispatch } from '../../hooks/redux';
import { fetchWorkflows, deleteWorkflow, createWorkflow } from '../../store/slices/workflowSlice';
import { Plus, Play, Pause, Copy, Trash2, Edit, MoreHorizontal } from 'lucide-react';

const WorkflowsPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { workflows, isLoading, error } = useAppSelector((state) => state.workflow);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newWorkflow, setNewWorkflow] = useState({ name: '', description: '' });

  useEffect(() => {
    dispatch(fetchWorkflows());
  }, [dispatch]);

  const handleCreateWorkflow = async () => {
    if (newWorkflow.name.trim()) {
      await dispatch(createWorkflow({
        name: newWorkflow.name,
        description: newWorkflow.description,
      }));
      setNewWorkflow({ name: '', description: '' });
      setShowCreateModal(false);
    }
  };

  const handleDeleteWorkflow = async (id: string) => {
    if (window.confirm(t('workflows.confirmDelete'))) {
      await dispatch(deleteWorkflow(id));
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active': return 'bg-green-100 text-green-800';
      case 'Draft': return 'bg-gray-100 text-gray-800';
      case 'Inactive': return 'bg-yellow-100 text-yellow-800';
      case 'Error': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            {t('workflows.title')}
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            {t('workflows.description')}
          </p>
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          <Plus className="h-4 w-4 mr-2" />
          {t('workflows.createNew')}
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      <div className="bg-white dark:bg-gray-800 shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          {workflows.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-gray-400 mb-4">
                <Play className="h-12 w-12 mx-auto" />
              </div>
              <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                {t('workflows.noWorkflows')}
              </h3>
              <p className="text-gray-600 dark:text-gray-400 mb-4">
                {t('workflows.noWorkflowsDescription')}
              </p>
              <button
                onClick={() => setShowCreateModal(true)}
                className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700"
              >
                <Plus className="h-4 w-4 mr-2" />
                {t('workflows.createFirst')}
              </button>
            </div>
          ) : (
            <div className="overflow-hidden">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-700">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('workflows.name')}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('workflows.status')}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('workflows.executions')}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('workflows.lastExecuted')}
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('workflows.actions')}
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                  {workflows.map((workflow) => (
                    <tr key={workflow.id} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div>
                          <div className="text-sm font-medium text-gray-900 dark:text-white">
                            {workflow.name}
                          </div>
                          <div className="text-sm text-gray-500 dark:text-gray-400">
                            {workflow.description}
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusColor(workflow.status)}`}>
                          {workflow.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                        {workflow.executionCount}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                        {workflow.lastExecutedAt ? new Date(workflow.lastExecutedAt).toLocaleDateString() : t('workflows.never')}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <div className="flex items-center justify-end space-x-2">
                          <button
                            className="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300"
                            title={t('workflows.edit')}
                          >
                            <Edit className="h-4 w-4" />
                          </button>
                          <button
                            className="text-green-600 hover:text-green-900 dark:text-green-400 dark:hover:text-green-300"
                            title={t('workflows.execute')}
                          >
                            <Play className="h-4 w-4" />
                          </button>
                          <button
                            className="text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-300"
                            title={t('workflows.clone')}
                          >
                            <Copy className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => handleDeleteWorkflow(workflow.id)}
                            className="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300"
                            title={t('workflows.delete')}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {showCreateModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white dark:bg-gray-800">
            <div className="mt-3">
              <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-4">
                {t('workflows.createNew')}
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('workflows.name')}
                  </label>
                  <input
                    type="text"
                    value={newWorkflow.name}
                    onChange={(e) => setNewWorkflow({ ...newWorkflow, name: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
                    placeholder={t('workflows.namePlaceholder')}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('workflows.description')}
                  </label>
                  <textarea
                    value={newWorkflow.description}
                    onChange={(e) => setNewWorkflow({ ...newWorkflow, description: e.target.value })}
                    rows={3}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
                    placeholder={t('workflows.descriptionPlaceholder')}
                  />
                </div>
              </div>
              <div className="flex justify-end space-x-3 mt-6">
                <button
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 dark:bg-gray-600 dark:text-gray-300 dark:border-gray-500 dark:hover:bg-gray-700"
                >
                  {t('common.cancel')}
                </button>
                <button
                  onClick={handleCreateWorkflow}
                  disabled={!newWorkflow.name.trim()}
                  className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {t('workflows.create')}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default WorkflowsPage;
