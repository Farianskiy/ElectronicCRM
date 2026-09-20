export const POLES_CHARACTERISTIC_CODE = "POLES";

export const POLE_CONFIGURATION_OPTIONS = [
  { value: "1P", label: "1P — один защищённый полюс" },
  { value: "1P+N", label: "1P+N — защищённая фаза и коммутируемая нейтраль" },
  { value: "2P", label: "2P — два защищённых полюса" },
  { value: "3P", label: "3P — три защищённых полюса" },
  { value: "3P+N", label: "3P+N — три защищённые фазы и коммутируемая нейтраль" },
  { value: "4P", label: "4P — четыре защищённых полюса" },
] as const;