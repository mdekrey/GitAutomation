import { Injector } from "./Injector";
import { Resolvers } from "./Resolvers";
import { InjectorBuilder } from "./InjectorBuilder";

const wheel: unique symbol = Symbol();
class Wheel {
  public readonly wheel = wheel;
}
class Vehicle {
  constructor(public readonly track: Track, public readonly wheels: Wheel[]) {}
}
const track: unique symbol = Symbol();
class Track {
  public readonly track = track;
}

enum Scope {
  Singleton = "Singleton",
  Race = "Race",
}
const scopeHierarchy = [Scope.Singleton, Scope.Race];

interface Services {
  wheel: Wheel;
  motorcycleFactory: () => Vehicle;
  carFactory: () => Vehicle;
  playerVehicle: Vehicle;
  opponentVehicle: Vehicle;
  track: Track;
  contestants: Vehicle[];
}

const builder = new InjectorBuilder<Services, Scope>(scopeHierarchy)
  .set("wheel", null, () => new Wheel())
  .set("motorcycleFactory", null, resolver => () =>
    new Vehicle(resolver("track"), [resolver("wheel"), resolver("wheel")])
  )
  .set("carFactory", null, resolver => () =>
    new Vehicle(resolver("track"), [
      resolver("wheel"),
      resolver("wheel"),
      resolver("wheel"),
      resolver("wheel"),
    ])
  )
  .set("playerVehicle", Scope.Singleton, resolver =>
    resolver("motorcycleFactory")()
  )
  .set("opponentVehicle", Scope.Race, resolver => resolver("carFactory")())
  .set("track", Scope.Singleton, resolver => new Track())
  .set("contestants", null, resolver => [
    resolver("playerVehicle"),
    resolver("opponentVehicle"),
  ]);

it("can create an injector", async () => {
  const target = builder.build();
});

it("can resolve transients without starting a scope", async () => {
  const target = builder.build();
  const actual = target.resolve("wheel");
  expect(actual).toBeInstanceOf(Wheel);
});

it("resolves transients each time", async () => {
  const target = builder.build();
  const actual = target.resolve("wheel");
  const actual2 = target.resolve("wheel");
  expect(actual).not.toBe(actual2);
  expect(actual).toBeInstanceOf(Wheel);
  expect(actual2).toBeInstanceOf(Wheel);
});

it("cannot resolve a scoped variable before starting the scope", async () => {
  const target = builder.build();
  expect(() => target.resolve("playerVehicle")).toThrowErrorMatchingSnapshot();
});

it("can resolve a scoped variable after starting the scope", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const actual = target.resolve("playerVehicle");
  expect(actual).toBeInstanceOf(Vehicle);
});

it("can resolve a scoped variable as the same repeatedly", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const actual = target.resolve("playerVehicle");
  const actual2 = target.resolve("playerVehicle");
  expect(actual).toBe(actual2);
});

it("can have multiple scopes", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
});

it("resolves similar types differently", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
  const actual = target.resolve("playerVehicle");
  const actual2 = target.resolve("opponentVehicle");
  expect(actual).not.toBe(actual2);
});

it("resolves a different variable after ending scope and restarting", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
  const actual = target.resolve("opponentVehicle");
  target.endScope(Scope.Race);
  target.beginScope(Scope.Race);
  const actual2 = target.resolve("opponentVehicle");
  expect(actual).not.toBe(actual2);
});

it("cannot resolve a service after ending scope", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
  target.resolve("opponentVehicle");
  target.endScope(Scope.Race);
  expect(() =>
    target.resolve("opponentVehicle")
  ).toThrowErrorMatchingSnapshot();
});

it("cannot resolve a service in a lower scope", async () => {
  const target = builder
    .copy()
    .set("track", Scope.Race, builder.getResolver("track"))
    .build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
  expect(() => target.resolve("playerVehicle")).toThrowErrorMatchingSnapshot();
});

it("transients can resolve services in any scope", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  target.beginScope(Scope.Race);
  const contestants = target.resolve("contestants");
  const opponent = target.resolve("opponentVehicle");
  const player = target.resolve("playerVehicle");
  expect(contestants).toContain(opponent);
  expect(contestants).toContain(player);
});

it("shares objects with parent injectors scopes", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const child1 = target.createChildInjector();
  const player1 = child1.resolve("playerVehicle");
  const child2 = target.createChildInjector();
  const player2 = child2.resolve("playerVehicle");
  expect(player1).toBe(player2);
});

it("can have separate scopes from child injectors", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const child1 = target.createChildInjector();
  child1.beginScope(Scope.Race);
  expect(() =>
    target.resolve("opponentVehicle")
  ).toThrowErrorMatchingSnapshot();
});

it("preserves own scope when child injectors are used", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const child1 = target.createChildInjector();
  child1.beginScope(Scope.Race);
  const opponent1 = child1.resolve("opponentVehicle");
  const child2 = target.createChildInjector();
  child2.beginScope(Scope.Race);
  const opponent2 = child1.resolve("opponentVehicle");
  expect(opponent1).toBe(opponent2);
});

it("can have separate scopes when child injectors are used", async () => {
  const target = builder.build();
  target.beginScope(Scope.Singleton);
  const child1 = target.createChildInjector();
  child1.beginScope(Scope.Race);
  const opponent1 = child1.resolve("opponentVehicle");
  const child2 = target.createChildInjector();
  child2.beginScope(Scope.Race);
  const opponent2 = child2.resolve("opponentVehicle");
  expect(opponent1).not.toBe(opponent2);
  expect(opponent1.track).toBe(opponent2.track);
});
