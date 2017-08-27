#!/bin/bash

mkdir /demo-repo
cd /demo-repo
git init

git config user.email "unit-test@example.com"
git config user.name "Unit Tester"

# This script creates a few branches for various scenarios:
# original - can be treated as a service line; is the zero-state repository
# initial - initial "infrastructure"
# better-readme - feature branch off of initial
# conflicting-readme - feature branch off of initial that conflicts with better-readme
# additional-content - feature branch off of initial
# future-readme - feature branch off of initial
# conflicts-resolved - a branch that resolves conflicts between better-readme and conflicting-readme

git checkout -B original
git checkout -B initial
echo "Simple test" > readme.md
git add .
git commit -m "Initial"

git checkout -B better-readme
printf "# Unit testing\n\nJust a simple repository to help with unit testing" > readme.md
git add .
git commit -m "Better Readme"

git checkout initial
git checkout -B conflicting-readme
echo "Continue" >> readme.md
git add .
git commit -m "Better Readme"

git checkout initial
git checkout -B additional-content
echo "This should merge in with no problem" > additional.md
git add .
git commit -m "Additional Content"

git checkout initial
git checkout -B future-feature
echo "This feature should get left behind" > future.md
git add .
git commit -m "Future feature"

git checkout better-readme
git checkout -B conflicts-resolved
git merge conflicting-readme -s ours
