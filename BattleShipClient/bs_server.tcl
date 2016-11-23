catch {console show}
console eval "wm minsize . 90 30"
console title "Battleship Server"
wm withdraw .
console hide

puts "Starting..."
# server

set ::clients 0
set ::player1done 0
set ::player2done 0

proc Server {startTime channel clientaddr clientport} {
    global player1
    global player2
    
    incr ::clients
    puts "Connection $::clients from $clientaddr registered"
    if {$::clients == 1} {
        puts $channel "Client $::clients"
        initplayer player1 player2 $channel
    } elseif {$::clients == 2} {
        puts $channel "Client $::clients"
        initplayer player2 player1 $channel
        after 1000 play
    }
    flush $channel
}

proc init {p} {
    upvar #0 $p player

    set player(shots) 0
    set player(hits) 0
    set player(misses) 0
    
    for {set i 1} {$i < 6} {incr i} { set player(boathits,$i) 0 }

    for {set i 0} {$i < 100} {incr i} {
        set player(radar,$i) 0
        set player(map,$i) 0
    }
}

proc display {p} {
    upvar #0 $p player
    
    set r 0; set m 0
    
    puts "$p radar"
    for {set i 0} {$i < 10} {incr i} {              ;# 10 rows
        for {set j 0} {$j < 10} {incr j} {
            puts -nonewline [format "%3d " $player(radar,$r)]
            incr r
        }
        puts -nonewline "    "
        for {set j 0} {$j < 10} {incr j} {
            puts -nonewline [format "%3d " $player(map,$m)]
            incr m
        }
        puts ""
    }
    puts "\nshots $player(shots), hits $player(hits), misses $player(misses)"
    update
}

proc initplayer {p1 p2 chan} {
    upvar #0 $p1 us
    upvar #0 $p2 them

    puts "$p1 starting"
# place player boats
    for {set i 1} {$i < 6} {incr i} { placeboat $p1 $i }    ;# place boats random
    state $p1 "OK " $chan
    
    set us(chan) $chan          ;# save channel for later
}

proc play {} {
    global player1
    global player2
    
    set ::gameover 0
    while {$::gameover == 0} {
        move player1 player2        ;# player1 move
        if {$::gameover} { return }
        move player2 player1        ;# player2 move
    }
}

proc move {p1 p2} {
    upvar #0 $p1 us
    upvar #0 $p2 them
    variable boatlen

    puts "$p1 move"
    state $p1 "SHOOT " $us(chan)        ;# send request

    gets $us(chan) line                 ;# get player shot 0-99
    
    set boatpos [lindex $line 0]        ;# just 0-99
    if {$boatpos < 0} {
        puts $us(chan) "ERROR - $line"
    } elseif {$boatpos > 99} {
        puts $us(chan) "ERROR - $line"
    } else {
        shoot $p1 $p2 $boatpos          ;# shoot at boatpos
        set boat $us(radar,$boatpos)    ;# result
        if {$us(hits) == 17} {          ;# max hits for all boats
            state $p1 "WIN " $us(chan)      ;# yahoo!
            state $p2 "LOSE " $them(chan)   ;# argh
            puts "$p1 Wins!"
            set ::gameover 1
#            close $us(chan)
#            close $them(chan)
        } elseif {$boat == -99} {
            state $p1 "MISS " $us(chan)
        } elseif {$us(boathits,$boat) == $boatlen($boat)} {
            state $p1 "SUNK $boat " $us(chan)
        } elseif {$boat > 0} {
            state $p1 "HIT " $us(chan)
        }
        incr us(shots)                  ;# count our shot
    }
    display $p1
}

proc state {p s chan} {
    upvar #0 $p player
    
    puts -nonewline $chan $s
    for {set i 0} {$i < 100} {incr i} { puts -nonewline $chan "$player(radar,$i) " }
    for {set i 0} {$i < 100} {incr i} { puts -nonewline $chan "$player(map,$i) " }
    puts $chan ""
    flush $chan
}

proc shoot {p1 p2 boatpos} {
    upvar #0 $p1 us
    upvar #0 $p2 them

# determine hit or miss and mark it
    set boat $them(map,$boatpos)
    if {$boat > 0} {                                ;# hit
        incr us(boathits,$boat)                     ;# count the hit on boat
        set them(map,$boatpos) [expr {-1 * $boat}]  ;# mark hit as -boat
        set us(radar,$boatpos) $boat                ;# our copy
        incr us(hits)                               ;# count our hits
    } elseif {$boat == 0} {                         ;# miss
        set them(map,$boatpos) -99
        set us(radar,$boatpos) -99
        incr us(misses)
    }
}

# randomly place boat number boat in players map
proc placeboat {p boat} {
    upvar #0 $p player
    variable boatlen
    
    set boatpos false               ;# boat not positioned
    while {$boatpos == false} {
        set boatstart [random 99]   ;# start position
        set boatdir [random 1]      ;# 0=horizontal, 1=vertical
        set boatpos true            ;# guess ok
        
        switch $boatdir {
            0 {
                set n [expr {$boatstart % 10}]          ;# isolate this horizontal row
                if {[expr {$n + $boatlen($boat)}] > 10} {
                    set boatpos false                   ;# no fit
                } else {
                    for {set i 0} {$i < $boatlen($boat)} {incr i} {
                        set bpos [expr {$boatstart + $i}]
                        if {$player(map,$bpos) != 0} { set boatpos false }  ;# space occupied
                    }
                }
            }
            1 {
                set n [expr {int($boatstart / 10)}]      ;# isolate this vertical column
                if {[expr {$n + $boatlen($boat)}] > 10} {
                    set boatpos false                   ;# no fit
                } else {
                    for {set i 0} {$i < $boatlen($boat)} {incr i} {
                        set bpos [expr {$boatstart + ($i * 10)}]
                        if {$player(map,$bpos) != 0} { set boatpos false }  ;# space occupied
                    }
                }
            }
        }
    }

;# successful placement at BoatStart & BoatDir, record it in player map
    switch $boatdir {
        0 {
            for {set i 0} {$i < $boatlen($boat)} {incr i} {
                set bpos [expr {$boatstart + $i}]
                set player(map,$bpos) $boat
            }
        }
        1 {
            for {set i 0} {$i < $boatlen($boat)} {incr i} {
                set bpos [expr {$boatstart + ($i * 10)}]
                set player(map,$bpos) $boat
            }
        }
    }
}

# return random number from 0 to n inclusive
proc random n {
    return [expr {int(rand() * ($n + 1))}]
}

# set up vars
variable boatlen

set boatlen(1) 5
set boatlen(2) 4
set boatlen(3) 3
set boatlen(4) 3
set boatlen(5) 2

global player1; init player1; set player1(me) 1
global player2; init player2; set player2(me) 2

# ok go
set portToUse 0

if { $argc >= 1 } {
	set portToUse [lindex $argv 0]
} else {
	set portToUse 9900
}
socket -server [list Server [clock seconds]] $portToUse
vwait forever

# testing
play player1 player2 stdout; display player1
play player2 player1 stdout; display player2

for {set i 0} {$i < 5} {incr i} {
    shoot player1 player2 [random 99]
    display player1; display player2
    update
}







