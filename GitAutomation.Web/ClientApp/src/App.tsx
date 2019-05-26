import React from "react";
import { Route, Switch } from "react-router";
import { Layout } from "./components/Layout";
import { Home } from "./components/Home";
import {
  CreateReserve,
  ReserveFlows,
  ReserveList,
} from "./components/reserves";
import {} from "./components/reserves";
import "./App.css";

export function App() {
  const prerenderedLoginScreen = null; // TODO - authentication
  return (
    <Layout>
      <Switch>
        {prerenderedLoginScreen}
        <Route exact path="/" component={Home} />
        <Route exact path="/create-reserve" component={CreateReserve} />
        <Route exact path="/reserve-flows" component={ReserveFlows} />
        <Route exact path="/reserves" component={ReserveList} />
      </Switch>
    </Layout>
  );
}
