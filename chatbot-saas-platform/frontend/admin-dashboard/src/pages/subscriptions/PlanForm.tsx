import React, { useEffect, useState } from "react";
import { Button } from "../../components/ui/button";

type Feature = {
  key: string;
  name: string;
  description: string;
  isEnabled: boolean;
  limit?: number;
  configuration: Record<string, any>;
};

const planTypeMap: Record<string, number> = {
  Standard: 0,
  Premium: 1,
  Enterprise: 2,
};

interface PlanFormProps {
  initialData?: any;
  mode: "create" | "edit" | "view";
  onSubmit: (plan: any) => void;
  onCancel: () => void;
  t: any;
}

const PlanForm: React.FC<PlanFormProps> = ({ initialData, mode, onSubmit, onCancel, t }) => {
  const [form, setForm] = useState({
    name: "",
    description: "",
    monthlyPrice: 0,
    yearlyPrice: 0,
    currency: "USD",
    isActive: true,
    isPublic: true,
    type: "Standard",
    features: {},
    limits: {},
    stripePriceIdMonthly: "",
    stripePriceIdYearly: "",
    trialDays: 14,
    sortOrder: 0,
    ...(initialData || {}),
  });
  const [featureList, setFeatureList] = useState<Feature[]>(
    initialData && initialData.features && typeof initialData.features === "object" && !Array.isArray(initialData.features)
      ? Object.entries(initialData.features).map(([key, f]: any) => ({
          key,
          name: f.name,
          description: f.description,
          isEnabled: f.isEnabled,
          limit: f.limit,
          configuration: f.configuration,
        }))
      : []
  );
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    const featuresObj: Record<string, any> = {};
    featureList.forEach((f) => {
      featuresObj[f.key] = {
        name: f.name,
        description: f.description,
        isEnabled: f.isEnabled,
        limit: f.limit,
        configuration: f.configuration,
      };
    });
    setForm((prev: typeof form) => ({ ...prev, features: featuresObj }));
  }, [featureList]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    let fieldValue: any = value;
    if (type === "checkbox") {
      fieldValue = (e.target as HTMLInputElement).checked;
    }
    setForm((prev: typeof form) => ({
      ...prev,
      [name]: fieldValue,
    }));
  };

  const handleFeatureChange = (idx: number, field: string, value: any) => {
    setFeatureList((prev) => prev.map((f, i) => (i === idx ? { ...f, [field]: value } : f)));
  };

  const addFeature = () => {
    setFeatureList((prev) => [
      ...prev,
      { key: `key_${Date.now()}`, name: "", description: "", isEnabled: false, limit: undefined, configuration: {} },
    ]);
  };

  const removeFeature = (idx: number) => {
    setFeatureList((prev) => prev.filter((_, i) => i !== idx));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    try {
      const planToSend = {
        ...form,
        type: planTypeMap[form.type] ?? 0,
      };
      onSubmit(planToSend);
    } catch (err: any) {
      setFormError(err.message || "Failed to save plan");
    }
  };

  const isView = mode === "view";
  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <label className="block">
        {t("subscriptions.fields.name")}
        <input
          name="name"
          value={form.name}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.name")}
          required
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.description")}
        <textarea
          name="description"
          value={form.description}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.description")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.monthlyPrice")}
        <input
          name="monthlyPrice"
          type="number"
          value={form.monthlyPrice}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.monthlyPrice")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.yearlyPrice")}
        <input
          name="yearlyPrice"
          type="number"
          value={form.yearlyPrice}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.yearlyPrice")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.currency")}
        <input
          name="currency"
          value={form.currency}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.currency")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.stripePriceIdMonthly")}
        <input
          name="stripePriceIdMonthly"
          value={form.stripePriceIdMonthly}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.stripePriceIdMonthly")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.stripePriceIdYearly")}
        <input
          name="stripePriceIdYearly"
          value={form.stripePriceIdYearly}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.stripePriceIdYearly")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.trialDays")}
        <input
          name="trialDays"
          type="number"
          value={form.trialDays}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.trialDays")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="block">
        {t("subscriptions.fields.sortOrder")}
        <input
          name="sortOrder"
          type="number"
          value={form.sortOrder}
          onChange={handleChange}
          placeholder={t("subscriptions.fields.sortOrder")}
          className="w-full border p-2 mt-1"
          disabled={isView}
        />
      </label>
      <label className="flex items-center">
        <input type="checkbox" name="isActive" checked={form.isActive} onChange={handleChange} className="mr-2" disabled={isView} />
        {t("subscriptions.fields.isActive")}
      </label>
      <label className="flex items-center">
        <input type="checkbox" name="isPublic" checked={form.isPublic} onChange={handleChange} className="mr-2" disabled={isView} />
        {t("subscriptions.fields.isPublic")}
      </label>
      <label className="block">
        {t("subscriptions.fields.type")}
        <select name="type" value={form.type} onChange={handleChange} className="border p-2 mt-1 w-full" disabled={isView}>
          <option value="Standard">{t("subscriptions.planTypes.standard")}</option>
          <option value="Premium">{t("subscriptions.planTypes.premium")}</option>
          <option value="Enterprise">{t("subscriptions.planTypes.enterprise")}</option>
        </select>
      </label>
      <label className="block font-bold mt-4">{t("subscriptions.fields.features")}</label>
      {featureList.map((feature, idx) => (
        <div key={feature.key} className="border p-2 mb-2 rounded bg-gray-50">
          <div className="flex gap-2 mb-2">
            <label className="flex-1">
              {t("subscriptions.fields.featureName")}
              <input
                className="w-full border p-1 mt-1"
                value={feature.name}
                onChange={(e) => handleFeatureChange(idx, "name", e.target.value)}
                placeholder={t("subscriptions.fields.featureName")}
                disabled={isView}
              />
            </label>
            <label className="flex-1">
              {t("subscriptions.fields.featureDescription")}
              <input
                className="w-full border p-1 mt-1"
                value={feature.description}
                onChange={(e) => handleFeatureChange(idx, "description", e.target.value)}
                placeholder={t("subscriptions.fields.featureDescription")}
                disabled={isView}
              />
            </label>
          </div>
          <div className="flex gap-2 mb-2">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={feature.isEnabled}
                onChange={(e) => handleFeatureChange(idx, "isEnabled", e.target.checked)}
                className="mr-2"
                disabled={isView}
              />
              {t("subscriptions.fields.featureIsEnabled")}
            </label>
            <label>
              {t("subscriptions.fields.featureLimit")}
              <input
                type="number"
                className="border p-1 ml-2"
                value={feature.limit ?? ""}
                onChange={(e) => handleFeatureChange(idx, "limit", e.target.value ? Number(e.target.value) : undefined)}
                placeholder={t("subscriptions.fields.featureLimit")}
                disabled={isView}
              />
            </label>
            {!isView && (
              <Button type="button" variant="destructive" size="sm" onClick={() => removeFeature(idx)}>
                {t("common.remove")}
              </Button>
            )}
          </div>
        </div>
      ))}
      {!isView && (
        <Button type="button" variant="outline" onClick={addFeature}>
          {t("subscriptions.fields.addFeature")}
        </Button>
      )}
      {formError && <div className="text-red-500">{formError}</div>}
      <div className="flex gap-2 mt-4">
        {!isView && <Button type="submit">{t("common.save")}</Button>}
        <Button type="button" variant="outline" onClick={onCancel}>
          {t("common.cancel")}
        </Button>
      </div>
    </form>
  );
};

export default PlanForm;
