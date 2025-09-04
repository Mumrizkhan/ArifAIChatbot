import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { useTranslation } from "react-i18next";
import { updateBrandingWithTranslations } from "../store/slices/themeSlice";
import { AppDispatch } from "../store/store";

export const useTranslatedTheme = () => {
  const { t, i18n } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();

  useEffect(() => {
    // Update theme branding with current translations
    dispatch(
      updateBrandingWithTranslations({
        welcomeMessage: t("widget.welcomeMessage"),
        placeholderText: t("widget.placeholder"),
      })
    );
  }, [i18n.language, t, dispatch]);

  return {
    updateBrandingTranslations: () => {
      dispatch(
        updateBrandingWithTranslations({
          welcomeMessage: t("widget.welcomeMessage"),
          placeholderText: t("widget.placeholder"),
        })
      );
    },
  };
};
