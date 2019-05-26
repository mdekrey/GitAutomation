export function groupBy<TInput>(
  input: TInput[],
  keySelector: (v: TInput) => string | string[]
): Record<string, TInput[]>;
export function groupBy<TInput, TOutput>(
  input: TInput[],
  keySelector: (v: TInput) => string | string[],
  valueSelector: (v: TInput) => TOutput
): Record<string, TOutput[]>;
export function groupBy<TInput, TOutput>(
  input: TInput[],
  keySelector: (v: TInput) => string | string[],
  maybeValueSelector?: (v: TInput) => TOutput
) {
  const valueSelector = (maybeValueSelector ||
    ((((v: TInput) => v) as unknown) as typeof maybeValueSelector))!;
  const groups = input.reduce<Record<string, TOutput[]>>((prev, current) => {
    const keyOrKeys = keySelector(current);
    const keys = Array.isArray(keyOrKeys) ? keyOrKeys : [keyOrKeys];
    const value = valueSelector(current);
    for (const key of keys) {
      const target = prev[key] || [];
      prev[key] = target;
      target.push(value);
    }
    return prev;
  }, {});
  return groups;
}
