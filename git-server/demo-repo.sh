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
# feature/enhanced-content - branch off of additional-content that is desireable
# feature/garbage-content - branch off of additional-content that will conflict
# feature/all-garbage - branch that contains two branches that will conflict with desirable results
# future-readme - feature branch off of initial
# readme-conflicts-resolved - a branch that resolves conflicts between better-readme and conflicting-readme
# content-conflicts-resolved - a branch that resolves conflicts between enhanced-content and garbage-content

git checkout -B original
touch readme.md
git add .
git commit -m "Original"

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
git commit -m "Worse Readme"

git checkout initial
git checkout -B additional-content
echo "This should merge in with no problem" > additional.md
git add .
git commit -m "Additional Content"

git checkout additional-content
git checkout -B feature/garbage-content
echo "\nJust keep making trash" >> additional.md
git add .
git commit -m "Garbage Content"

git checkout additional-content
git checkout -B feature/enhanced-content
git merge better-readme
echo "\nThis builds on top of additional" >> additional.md
git add .
git commit -m "Enhanced Content"

git checkout feature/garbage-content
git checkout -B feature/all-garbage
git merge conflicting-readme
echo "\nMore conflicts" >> additional.md
git add .
git commit -m "All Garbage Content"

git checkout initial
git checkout -B future-feature
echo "This feature should get left behind" > future.md
git add .
git commit -m "Future feature"

git checkout better-readme
git checkout -B readme-conflicts-resolved
git merge conflicting-readme -s ours

git checkout feature/enhanced-content
git checkout -B content-conflicts-resolved
git merge feature/garbage-content -s ours
