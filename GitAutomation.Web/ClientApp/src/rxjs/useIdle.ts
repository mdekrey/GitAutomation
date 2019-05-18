import { useState, useEffect } from "react";

export enum IdleState {
  InitialIdle,
  Loading,
  Loaded,
}

export const useIdle = <T>(targets: any[]) => {
  const [state, setState] = useState(IdleState.InitialIdle);
  useEffect(() => {
    if (state !== IdleState.InitialIdle) {
      return () => {};
    }
    const timeout = setTimeout(() => {
      if (state === IdleState.InitialIdle) {
        setState(IdleState.Loading);
      }
    }, 140);
    return () => clearTimeout(timeout);
  }, [state]);
  useEffect(() => {
    if (!targets.some(t => t === undefined)) {
      setState(IdleState.Loaded);
    }
    // eslint-disable-next-line
  }, targets);
  return state;
};
