import { render } from "@testing-library/react";
import { Provider } from "react-redux";
import { store } from "@/store/store";
import type { ReactElement, ReactNode } from "react";

function ReduxProvider({ children }: { children: ReactNode }) {
  return <Provider store={store}>{children}</Provider>;
}

export function renderWithProviders(ui: ReactElement) {
  return render(ui, {
    wrapper: ReduxProvider,
  });
}
