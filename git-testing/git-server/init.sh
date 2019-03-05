#!/bin/bash

#set to a known user/pass for testing
ng-auth -u tester -p TEST_PASSWORD

# Sets up three duplicates of the same repository for easy unit testing of various scenarios
repo-admin -n gittesting1 -d "Testing Git"
cd /demo-repo
git remote add origin1 /repos/gittesting1.git
git push --all origin1
cd /repos/gittesting1.git
git symbolic-ref HEAD refs/heads/original

repo-admin -n gittesting2 -d "Testing Git"
cd /demo-repo
git remote add origin2 /repos/gittesting2.git
git push --all origin2
cd /repos/gittesting2.git
git symbolic-ref HEAD refs/heads/original

repo-admin -n gittesting3 -d "Testing Git"
cd /demo-repo
git remote add origin3 /repos/gittesting3.git
git push --all origin3
cd /repos/gittesting3.git
git symbolic-ref HEAD refs/heads/original
