import React from "react";
import { Route, Switch } from "react-router";
import { Layout } from "./components/Layout";
import { Home } from "./components/Home";
import {
  CreateReserve,
  ReserveList,
  ReserveDetail,
} from "./components/reserves";
import { FlowDisplay } from "./components/flow";
import "./App.css";

export function App() {
  const prerenderedLoginScreen = null; // TODO - authentication
  return (
    <Layout>
      <Switch>
        {prerenderedLoginScreen}
        <Route exact path="/" component={Home} />
        <Route exact path="/create-reserve" component={CreateReserve} />
        <Route exact path="/reserve-flows" component={FlowDisplay} />
        <Route exact path="/reserves" component={ReserveList} />
        <Route exact path="/reserves/:reserve+" component={ReserveDetail} />
      </Switch>
    </Layout>
  );
}
