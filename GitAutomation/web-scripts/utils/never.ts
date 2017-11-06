export function neverEver(value: never): never {
  console.error(`${value} was not never.`);
  throw new Error(`${value} was not never.`);
}
