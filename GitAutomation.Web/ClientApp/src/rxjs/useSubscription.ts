import { Subscription } from "rxjs";
import { DependencyList, useEffect } from "react";

export const useSubscription = (
  subscriptionFactory: () => Subscription,
  deps: DependencyList
) => {
  useEffect(() => {
    const subscription = subscriptionFactory();
    return () => subscription.unsubscribe();
  }, deps);
};
