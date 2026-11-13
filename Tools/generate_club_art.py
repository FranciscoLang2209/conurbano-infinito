"""Original Club del Barrio sprites, using the project's pixel canvas."""
from generate_visual_slice import ART, Canvas
from generate_animation_frames import read_sprite


def portrait(source, path):
    c = Canvas(132)
    for y in range(44):
        for x in range(44):
            pixel = source.pixels[y][(x+2)*4:(x+3)*4]
            for dy in range(3):
                for dx in range(3):
                    c.pixels[y*3+dy][(x*3+dx)*4:(x*3+dx+1)*4] = pixel
    c.save(path)


def ball(size):
    c = Canvas(size)
    mid, radius = size // 2, size // 2 - 4
    c.ellipse(mid, mid, radius, radius, 'ink')
    c.ellipse(mid, mid, radius - 1, radius - 1, 'concrete')
    c.ellipse(mid - 1, mid - 2, radius - 2, radius - 3, 'cream')
    c.poly([(mid-4,mid-4),(mid+2,mid-5),(mid+6,mid),(mid+2,mid+5),(mid-4,mid+3)], 'ink')
    for dx, dy in ((-radius+3, -3), (4,-radius+3), (radius-4,7), (-5,radius-3)):
        x,y=mid+dx,mid+dy
        c.line(mid+dx//2,mid+dy//2,x,y,'shadow')
        c.poly([(x-2,y-2),(x+2,y-2),(x+3,y+1),(x,y+3),(x-3,y+1)],'ink')
    c.line(mid-radius+5,mid-7,mid-radius+7,mid-9,'white')
    c.line(mid+3,mid+radius-4,mid+7,mid+radius-6,'pavement')
    assert {a for row in c.pixels for a in row[3::4]} == {0,255}
    assert not any(c.pixels[0][3::4]) and not any(c.pixels[-1][3::4])
    assert all(row[3] == row[-1] == 0 for row in c.pixels)
    return c


def canchita():
    c = Canvas(192)
    c.ellipse(96, 140, 88, 32, 'grass_dark')
    c.rect(9, 42, 182, 154, 'ink')
    c.rect(11, 44, 180, 152, 'grass_dark')
    for x in range(13, 179, 24):
        c.rect(x, 46, min(x+11,178), 150, 'grass')
    c.rect(15, 48, 176, 49, 'cream')
    c.rect(15, 147, 176, 148, 'cream')
    c.rect(15, 48, 16, 148, 'cream')
    c.rect(175, 48, 176, 148, 'cream')
    c.rect(95, 49, 96, 147, 'cream')
    c.ellipse(96, 98, 25, 25, 'cream')
    c.ellipse(96, 98, 23, 23, 'grass_dark')
    c.rect(95, 76, 96, 120, 'cream')
    c.ellipse(96, 98, 2, 2, 'white')
    # The amber ring marks the trigger radius, independent of the pitch markings.
    for y in range(44,153):
        for x in range(42,151):
            distance=(x-96)**2+(y-98)**2
            if 51**2 <= distance <= 53**2 and ((x+y)//4)%2 == 0:
                c.dot(x,y,'yellow')
    for x, sign in ((16,1),(175,-1)):
        for y in (76,120): c.line(x,y,x+sign*28,y,'cream')
        c.line(x+sign*28,76,x+sign*28,120,'cream')
    for x in (7,177):
        c.rect(x,83,x+7,115,'ink')
        for y in range(86,114,5): c.line(x+1,y,x+6,y,'concrete')
        c.line(x+1,84,x+1,114,'white')
        c.line(x+6,84,x+6,114,'white')
        c.line(x+1,84,x+6,84,'cream')
    # A patched club banner and bench make it a neighborhood landmark.
    c.rect(24,19,168,22,'ink')
    c.rect(24,20,26,42,'ink')
    c.rect(166,20,168,42,'ink')
    c.rect(30,23,160,38,'blue_dark')
    for x in range(31,160,16): c.rect(x,24,x+7,37,'cyan')
    c.poly([(88,21),(104,21),(103,34),(96,40),(89,34)],'ink')
    c.poly([(90,23),(102,23),(101,33),(96,37),(91,33)],'cream')
    c.rect(94,24,97,32,'blue')
    c.rect(38,158,75,161,'ink')
    c.rect(39,158,74,159,'ochre')
    c.rect(42,162,44,169,'ink')
    c.rect(69,162,71,169,'ink')
    c.ellipse(145,165,6,6,'ink')
    c.ellipse(145,164,5,5,'cream')
    c.poly([(143,162),(147,162),(148,165),(145,167),(142,165)],'ink')
    from generate_plaza import litter, paper, bottle, crate
    for variant, (draw, x, y) in enumerate(((paper, 28, 175), (bottle, 112, 166),
                                           (crate, 174, 176), (paper, 86, 179))):
        litter(c, draw, x, y, variant)
    assert all(len(row)==192*4 for row in c.pixels)
    assert {a for row in c.pixels for a in row[3::4]} == {0,255}
    return c


def delivery_pois():
    """Only the three new delivery landmarks; leave all existing artwork untouched."""
    for name in ('MuralDelBarrio', 'RondaDeMate', 'Bicicleteria'):
        c = Canvas(96)
        c.ellipse(48, 65, 42, 20, 'grass_dark')
        if name == 'MuralDelBarrio':
            c.rect(10, 18, 85, 66, 'ink')
            c.rect(13, 21, 82, 63, 'concrete')
            for y in (30, 44, 58):
                c.line(13, y, 82, y, 'pavement')
            c.poly([(22, 51), (36, 27), (48, 49), (62, 28), (75, 54)], 'blue')
            c.ellipse(48, 33, 7, 7, 'yellow')
            c.rect(19, 56, 76, 59, 'red')
            c.rect(67, 68, 73, 76, 'blue_dark')
        elif name == 'RondaDeMate':
            for x in (16, 72):
                c.rect(x, 42, x+9, 71, 'ink')
                c.rect(x+1, 44, x+8, 64, 'ochre')
            c.ellipse(48, 53, 24, 15, 'ink')
            c.ellipse(48, 50, 21, 12, 'ochre')
            c.rect(37, 25, 46, 49, 'ink')
            c.rect(39, 27, 44, 47, 'cream')
            c.rect(39, 23, 45, 28, 'red')
            c.ellipse(58, 43, 7, 8, 'ink')
            c.ellipse(58, 41, 5, 5, 'grass')
            c.line(59, 39, 65, 27, 'cream')
        else:
            for x in (23, 73):
                c.ellipse(x, 59, 16, 16, 'ink')
                c.ellipse(x, 59, 13, 13, 'concrete')
                c.ellipse(x, 59, 11, 11, 'grass_dark')
            for a, b in (((23,59),(38,36)), ((38,36),(52,59)), ((52,59),(23,59)),
                         ((38,36),(62,36)), ((62,36),(52,59)), ((62,36),(73,59))):
                c.line(*a, *b, 'yellow')
            c.line(35, 32, 46, 32, 'ink')
            c.line(62, 36, 66, 27, 'cream')
            c.line(66, 27, 75, 27, 'cream')
            c.rect(10, 14, 31, 23, 'blue_dark')
            c.line(14, 18, 27, 18, 'cream')
        for y in range(96):
            for x in range(96):
                radius = (x-48)**2 + (y-50)**2
                if 39**2 <= radius <= 41**2 and ((x+y)//3)%2 == 0:
                    c.dot(x, y, 'yellow')
        assert {a for row in c.pixels for a in row[3::4]} == {0, 255}
        assert not any(c.pixels[0][3::4]) and not any(c.pixels[-1][3::4])
        c.save(ART / ('Environment/' + name + '.png'))


def main():
    # La Piba's edited PNGs are authoritative; do not overwrite them with the retired procedural design.
    portraits = ART.parents[1] / 'Resources/CharacterPortraits'
    portrait(read_sprite(ART / 'Player/ElPibeDelBarrio.png'), portraits / 'ElPibeDelBarrio.png')
    ball(48).save(ART / 'Weapons/PelotaDePotrero.png')
    ball(40).save(ART.parents[1] / 'Resources/BarrioUi/weapon_ball.png')
    canchita().save(ART / 'Environment/CanchitaDelClub.png')
    print('CLUB_ART_PASS piba=preserved portrait-pibe=1 pelota=48+40 canchita=192 palette=existing')


if __name__ == '__main__':
    main()
