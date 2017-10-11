#!/bin/bash

if [ -d "/extra-bins" ]; then
	ADDONS=$(ls /extra-bins)
	echo $ADDONS
fi
