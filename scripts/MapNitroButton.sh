#!/bin/bash

DEVICE=$(cat /proc/bus/input/devices | grep -A 5 -B 5 "keyboard\|Keyboard" | grep "event" | head -1 | sed 's/.*event\([0-9]*\).*/\/dev\/input\/event\1/')

# More efficient: exit after processing instead of continuous loop
evtest "$DEVICE" | grep --line-buffered "code 425.*value 1" | while read line; do
    DAMX &
done
