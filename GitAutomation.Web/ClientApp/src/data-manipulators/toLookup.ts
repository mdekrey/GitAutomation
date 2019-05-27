export function toLookup<TInput>(
  input: TInput[],
  keySelector: (v: TInput) => string
): Record<string, TInput>;
export function toLookup<TInput, TOutput>(
  input: TInput[],
  keySelector: (v: TInput) => string,
  valueSelector: (v: TInput) => TOutput
): Record<string, TOutput>;
export function toLookup<TInput, TOutput>(
  input: TInput[],
  keySelector: (v: TInput) => string,
  maybeValueSelector?: (v: TInput) => TOutput
) {
  const valueSelector = (maybeValueSelector ||
    ((((v: TInput) => v) as unknown) as typeof maybeValueSelector))!;
  const lookup = input.reduce<Record<string, TOutput>>((prev, current) => {
    const key = keySelector(current);
    const value = valueSelector(current);
    prev[key] = value;
    return prev;
  }, {});
  return lookup;
}
