catch {console show}
console eval "wm minsize . 90 30"
console title "Battleship Player"
wm withdraw .

puts "Starting..."

# return random number from 0 to n inclusive
proc random n {
    return [expr {int(rand() * ($n + 1))}]
}

proc shootaround {pos} {
    set boat [expr {abs($::radar($pos))}]   ;# hit on this boat
    set shootaround -1                      ;# assume no shot
    
    if {$::boathits($boat) == 1} {          ;# first hit, see which direction possible
        set len [expr {[open_left $pos] + [open_right $pos] + 1}]
        if {$len >= $::boatlen($boat)} {    ;# boat fits horizontally ...
            if {[val_left $pos] == 0} {
                set shootaround [incr pos -1]
            } else {
                set shootaround [incr pos]      ;# right has to be ok
            }
        } else {                                ;# has to fit vertical if not horizontal ...
            if {[val_up $pos] == 0} {
                set shootaround [incr pos -10]
            } else {
                set shootaround [incr pos 10]   ;# down has to be ok
            }
        }
    } elseif {$::boathits($boat) < $::boatlen($boat)} {     ;# second or more hit but not sunk, work same direction
        if {([expr {abs([val_left $pos])}] == $boat) && ([val_right $pos] == 0)} {
            set shootaround [incr pos]          ;# shoot right
        } elseif {([expr {abs([val_right $pos])}] == $boat) && ([val_left $pos] == 0)} {
            set shootaround [incr pos -1]       ;# shoot left
        } elseif {([expr {abs([val_up $pos])}] == $boat) && ([val_down $pos] == 0)} {
            set shootaround [incr pos 10]       ;# shoot down
        } elseif {([expr {abs([val_down $pos])}] == $boat) && ([val_up $pos] == 0)} {
            set shootaround [incr pos -10]      ;# shoot up
        }
    }
    return $shootaround
}

proc rankboat len {
    puts "rankboat $len"
    for {set pos 0} {$pos < 100} {incr pos} {
        if {$::radar($pos) == 0} {
            if {[expr {[open_right $pos] + 1}] >= $len} {
                for {set i $pos} {$i < [expr {$pos + $len}]} {incr i} {
                    incr ::rank($i)
                }
            }
            if {[expr {[open_down $pos] + 1}] >= $len} {
                for {set i $pos} {$i < [expr {$pos + ($len * 10)}]} {incr i 10} {
                    incr ::rank($i)
                }
            }
        }
    }
}

proc val_left {pos} {
    if {($pos % 10) == 0} {
        return -99              ;# edge of map
    } else {
        incr pos -1
        return $::radar($pos)   ;# return what's in left pos
    }
}

proc val_right {pos} {
    if {($pos % 10) == 9} {
        return -99              ;# edge of map
    } else {
        incr pos
        return $::radar($pos)   ;# return what's in right pos
    }
}

proc val_up {pos} {
    if {$pos < 10} {
        return -99              ;# edge of map
    } else {
        incr pos -10
        return $::radar($pos)   ;# return what's in up pos
    }
}

proc val_down {pos} {
    if {$pos > 89} {
        return -99              ;# edge of map
    } else {
        incr pos 10
        return $::radar($pos)   ;# return what's in down pos
    }
}

proc open_left {pos} {
    if {[val_left $pos] != 0} {
        return 0
    } else {
        incr pos -1
        return [expr {1 + [open_left $pos]}]
    }
}

proc open_right {pos} {
    if {[val_right $pos] != 0} {
        return 0
    } else {
        incr pos
        return [expr {1 + [open_right $pos]}]
    }
}

proc open_up {pos} {
    if {[val_up $pos] != 0} {
        return 0
    } else {
        incr pos -10
        return [expr {1 + [open_up $pos]}]
    }
}

proc open_down {pos} {
    if {[val_down $pos] != 0} {
        return 0
    } else {
        incr pos 10
        return [expr {1 + [open_down $pos]}]
    }
}

# vars
set ::boatlen(1) 5
set ::boatlen(2) 4
set ::boatlen(3) 3
set ::boatlen(4) 3
set ::boatlen(5) 2

# here we go
set port 9900                                   ;# default port
if {$argc} { set port [lindex $argv 0] }

set server localhost
set sockChan [socket $server $port]

fileevent $sockChan readable [list gets $sockChan ::line]

while {1} {
    vwait ::line
    
    set cmd [lindex $::line 0]
    if {$cmd == "xWIN"} {
        close $sockChan
        break
    } elseif {$cmd == "xLOSE"} {
        close $sockChan
        break
    } elseif {$cmd == "SHOOT"} {            ;# time to shoot
        for {set i 1} {$i < 6} {incr i} {
            set ::boathits($i) 0            ;# reset boat hits
        }

        for {set i 0} {$i < 100} {incr i} {
            set x [lindex $::line [expr {$i + 1}]]
            set ::radar($i) $x
            if {$x > 0} {                   ;# hit
                incr ::boathits($x)         ;# count
            }
            set ::rank($i) 0
        }

        set boatpos -1
        set n 0

# if we have a hit focus on sinking it
        while {($boatpos == -1) && ($n < 100)} {
#            if {($::radar($n) < 0) && ($::radar($n) != -99)} {}
            if {$::radar($n) > 0} {
                set boatpos [shootaround $n]
            }
            incr n
        }

# no intelligent shot, figure probabilities

        if {$boatpos == -1} {
            for {set i 0} {$i < 100} {incr i} {
                set ::rank($i) 0
            }

            for {set i 1} {$i < 6} {incr i} {           ;# loop through all boats and caculate ranks
                if {$::boathits($i) == 0} {             ;# boat remains
                    rankboat $::boatlen($i)
                }
            }

            set n 0
            for {set i 0} {$i < 100} {incr i} {
                if {$::rank($i) > $n} {
                    set n $::rank($i)                   ;# capture highest rank value
                    set m 1                             ;# and start count of max
                } elseif {$::rank($i) == $n} {
                    incr m                              ;# count it
                }
            }
            
# pick one of the highest randomly
            set m [expr {[random [expr {$m - 1}]] + 1}] ;# 1-?
            while {$m > 0} {
                incr boatpos
                if {$::rank($boatpos) == $n} { incr m -1 }   ;# right rank, count it
            }
        }

#diag
        set r 0 ; set m 0
        puts "radar"
        for {set i 0} {$i < 10} {incr i} {              ;# 10 rows
            for {set j 0} {$j < 10} {incr j} {
                puts -nonewline [format "%3d " $::radar($r)]
                incr r
            }
            puts -nonewline "    "
            for {set j 0} {$j < 10} {incr j} {
                puts -nonewline [format "%3d " $::rank($m)]
                incr m
            }
            puts ""
        }
        puts ""

        puts "shoot $boatpos"
        update

#    after 1000 {incr ::wait}
#    vwait ::wait

        puts $sockChan $boatpos
    } else {
        puts $line
    }
    flush $sockChan
}

puts "Done"

