import React from 'react';
import { Route, Switch } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';

export function App() {
  const prerenderedLoginScreen = null; // TODO - authentication
  return (
    <Layout>
      <Switch>
        {prerenderedLoginScreen}
        <Route exact path='/' component={Home} />
      </Switch>
    </Layout>
  );
}
