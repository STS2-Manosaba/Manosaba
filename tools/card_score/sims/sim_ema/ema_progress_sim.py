import random
# 艾瑪裁判進度：腿環Lv1 每場打出8張牌→得 RawTellOwk(0費，假設立刻打) 開審問；
# 之後每張裁判牌打出 +1 進度；>=10 觸發。統計「某張裁判牌被打出時進度已>=10/20」的比例。
def run(trial_frac, plays_mu, N=40000, seed=1):
    rnd = random.Random(seed)
    mix = [(0.70, (3, 5)), (0.20, (5, 7)), (0.10, (8, 11))]  # 小怪/菁英/首領回合數
    hit = tot = hit20 = 0
    for _ in range(N):
        r = rnd.random(); acc = 0
        for p, (a, b) in mix:
            acc += p
            if r <= acc:
                T = rnd.randint(a, b); break
        played = 0; started = False; prog = 0
        for t in range(T):
            n = max(2, int(round(rnd.gauss(plays_mu, 1))))
            for _ in range(n):
                if rnd.random() < trial_frac:
                    tot += 1; hit += prog >= 10; hit20 += prog >= 20
                    if started: prog += 1
                played += 1
                if not started and played >= 8:
                    started = True
    return hit / tot, hit20 / tot

for tf in (0.35, 0.45, 0.55):
    for mu in (3.5, 4.5):
        print(tf, mu, ["%.3f" % x for x in run(tf, mu)])
