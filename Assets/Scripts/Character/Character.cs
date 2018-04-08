﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Character : MonoBehaviour
{
    public int HealthPoints { get; set; }
    public int HealthCapacity { get; protected set; }

    public int BatteryCharge { get; protected set; }
    public int BatteryCapacity { get; protected set; }

    public Skill MainSkill { get; protected set; }
    public Skill SecondarySkill { get; protected set; }
    public Skill ThirdSkill { get; protected set; }

    public float MoveSpeed { get; protected set; }

    public int ShotDamage;

    [SerializeField] LayerMask shootMask;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] GameObject muzzle;
    [SerializeField] GameObject muzzle2;
    [SerializeField] protected GameObject bullet;
    [SerializeField] protected GameObject bullet2;
    public bool OnlinePlayer;
    public bool IsDead;

    float nextFire = 0f;
    float nextFire2 = 0f;

    protected Rigidbody2D rg;

    private Aim AimController;

    protected PlayerBattery plBattery;

    protected SynchronizationController syncController;
    private List<OnlineCharacter> onlinePlayersList;

    private bool summonInProgress;

    protected virtual void Start()
    {
        rg = GetComponent<Rigidbody2D>();
        AimController = GameObject.Find("Map").GetComponent<Aim>();

        plBattery = GetComponent<PlayerBattery>();
        syncController = FindObjectOfType<SynchronizationController>();

        summonInProgress = false;

    }


    public virtual void Move()
    {
        Vector2 rigV = Vector2.zero;

        //Player Movement
        if (Input.GetKey(KeyCode.W))
        {
            rigV.y = MoveSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            rigV.x = -MoveSpeed;
        }

        if (Input.GetKey(KeyCode.S))
        {
            rigV.y = -MoveSpeed;
        }

        if (Input.GetKey(KeyCode.D))
        {
            rigV.x = MoveSpeed;
        }

        rigV.Normalize();
        rigV *= MoveSpeed;
        rg.velocity = rigV;

    }

    protected virtual void Rotate()
    {
        //Player Rotation
        Vector3 objectPos = new Vector3(0, 0, 0);
        Vector3 dir = new Vector3(0, 0, 0);

        objectPos = Camera.main.WorldToScreenPoint(transform.position);

        if (!AimController.lockCursor)
        {
            dir = AimController.mPos - objectPos;
        }
        else
        {
            dir = AimController.worldLock - new Vector2(transform.position.x, transform.position.y);

            //Если расстояние больше 80 юнитов то обрываем лок курсора
            if (Vector2.Distance(transform.position, AimController.worldLock) > 80f)
                AimController.lockCursor = false;

        }



        transform.rotation = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90));
    }

    protected virtual void Update()
    {

        if (!OnlinePlayer && !IsDead)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot(bullet);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnMainSkillUse();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                OnSecondarySkillUse();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                OnThirdSkillUse();
            }
            if (Input.GetMouseButtonDown(1))
            {

                AimController.lockCursor = true;
                AimController.worldLock = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                AimController.lockCursor = false;
            }
        }
    }

    protected virtual void Shoot(GameObject bulletObject)
    {

        if (gameObject.tag == "Boss")
        {
            GameObject mf2 = Instantiate(muzzleFlash, muzzle2.transform.position, transform.rotation) as GameObject;
            mf2.transform.parent = transform;

            fireBullet2(bullet2);

            GameObject.Destroy(mf2, 0.1f);
        }

        GameObject mf = Instantiate(muzzleFlash, muzzle.transform.position, transform.rotation) as GameObject;
        mf.transform.parent = transform;

        fireBullet(bulletObject);

        GameObject.Destroy(mf, 0.1f);

    }

    protected virtual void fireBullet2(GameObject bulletObject)
    {

        if (Time.time > nextFire2)
        {
            nextFire2 = Time.time;

            bullet sBullet = Instantiate(bulletObject, muzzle2.transform.position, muzzle2.transform.rotation).GetComponent<bullet>();
            sBullet.Shooter = gameObject;
            sBullet.Damage = ShotDamage;

            Destroy(sBullet.gameObject, 1f);

            SocketController.RequestPlayerShot(new ShotData(SocketController.SocketId, muzzle.transform.position,
                muzzle.transform.rotation.eulerAngles));
        }

    }

    protected virtual void fireBullet(GameObject bulletObject)
    {

        if (Time.time > nextFire)
        {
            nextFire = Time.time;

            bullet sBullet = Instantiate(bulletObject, muzzle.transform.position, muzzle.transform.rotation).GetComponent<bullet>();
            sBullet.Shooter = gameObject;
            sBullet.Damage = ShotDamage;


            Destroy(sBullet.gameObject, 1f);

            SocketController.RequestPlayerShot(new ShotData(SocketController.SocketId, muzzle.transform.position,
                muzzle.transform.rotation.eulerAngles));
        }

    }

    protected virtual void FixedUpdate()
    {
        if (!OnlinePlayer && !IsDead)
        {
            Move();
            Rotate();
        }
    }

    protected virtual void OnMainSkillUse()
    {

    }

    protected virtual void OnSecondarySkillUse() { }

    protected virtual void OnThirdSkillUse()
    {

        if (summonInProgress)
            return;

        onlinePlayersList = syncController.OnlineCharacters.Where(x => x.Value.Character != "Boss")
            .Select(x => x.Value)
            .ToList();

        foreach (OnlineCharacter onPlayer in onlinePlayersList)
        {
            Teleport(onPlayer.gameObject);
            StartCoroutine(startSummon());
        }

    }

    private void Teleport(GameObject teleportGameObj)
    {

        if (plBattery.currentEnergy < 2)
            return;

        teleportGameObj.transform.position = new Vector3(transform.position.x, transform.position.y);
        plBattery.discharge(2);
    }

    IEnumerator startSummon()
    {

        summonInProgress = true;
        yield return new WaitForSeconds(5);
        summonInProgress = false;

    }
}
