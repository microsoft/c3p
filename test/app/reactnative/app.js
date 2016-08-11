// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

'use strict';

import React, {
  AppRegistry,
  Component,
} from 'react';

import {
  Text,
  View,
  WebView,
  StyleSheet,
} from 'react-native';

var tests = require('./tests');
var Button = require('react-native-button');
var plugin = require('c3p-test-reactnative');

class C3PTestApp extends Component {
  constructor(props) {
    super(props);
    this.state = {
      logHtml: "",
    };
  }

  render() {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.welcome}>
            C3P React Native Tests
          </Text>
          <Button
            containerStyle={styles.testButton}
            style={styles.testButtonText}
            onPress={this.onTestButtonClick.bind(this)}
          >
            Run Tests
          </Button>
        </View>
        <WebView
          style={styles.logView}
          source={{html: this.state.logHtml}}
          automaticallyAdjustContentInsets={false}
          scrollEnabled={true}
          javaScriptEnabled={true}
        />
      </View>
    );
  }

  logMessage(message, style) {
      this.logLines += "<span style='" + (style || "") + "'>" + message + "</span><br/>\n";
      var logHtml = "<html><body style='font-family: sans-serif; font-size: 9pt'" +
        " onload='scrollTo(0, document.body.scrollHeight)'>\n" +
        this.logLines + "</body></html>";
      this.setState({ logHtml: logHtml });
  }

  onTestButtonClick() {
      this.logLines = "";
      this.setState({logHtml: ""});
      tests.runTests(plugin, this.logMessage.bind(this));
  }
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'column',
    flex: 1,
  },
  header: {
    backgroundColor: '#E4E4FF',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
    flex: 0,
  },
  welcome: {
    color: '#000000',
    fontSize: 20,
    textAlign: 'center',
    margin: 12,
  },
  testButton: {
    backgroundColor: '#4682B4',
    padding: 10,
    margin: 10,
  },
  testButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: 'bold',
  },
  logView: {
    flex: 1,
  }
});

export { C3PTestApp };
