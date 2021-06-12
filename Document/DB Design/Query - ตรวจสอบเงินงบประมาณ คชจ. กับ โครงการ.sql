-- เงินงบประมาณที่ได้รับจากรัฐ รายการ คชจ. ที่เพิ่มโครงการไม่ได้
select sum(budget_amount) as estimate_budget_amount 
from t_budget_expenses where active =1 and CAN_ADD_PROJECT = 0 and yr = 2021;

-- เงินงบประมาณที่ได้รับจากรัฐ รายการ คชจ. ที่เพิ่มโครงการได้
select sum(budget_amount) as estimate_budget_amount 
from t_budget_expenses where active = 1 and CAN_ADD_PROJECT = 1 and yr = 2021;

-- เงินงบประมาณที่ได้รับจากรัฐ โครงการเงินงบประมาณ
select sum(budget_amount) as estimate_budget_amount 
from t_budget_expenses_project 
where active = 1 and yr = 2021 and project_for_type = 1;



-- เทียบเงินที่รัฐบาลจะจัดสรร ระหว่าง รายการค่าใช้จ่ายที่เพิ่มโครงการได้ และ โครงการ
select *
from(
select plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id
, sum(budget_amount) as estimate_budget_amount
, (
	select sum(budget_amount) from t_budget_expenses_project x 
	where x.yr = 2021 and x.project_for_type = 1 and x.active = 1
	and x.plan_id = a.plan_id
	and x.produce_id = a.produce_id
	and x.activity_id = a.activity_id
	and x.budget_type_id = a.budget_type_id
	and x.expenses_group_id = a.expenses_group_id
	and x.expenses_id = a.expenses_id
  ) as proj_estimate_budget_amount
from t_budget_expenses a where active = 1 and CAN_ADD_PROJECT = 1 and yr = 2021
group by plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id
) as final where estimate_budget_amount != proj_estimate_budget_amount;




-- เทียบเงินที่รัฐบาลจะจัดสรร ระหว่าง รายการค่าใช้จ่ายที่เพิ่มโครงการได้ และ โครงการ
select *
from(
select plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id
, sum(off_budget_amount) as estimate_off_budget_amount
, (
	select sum(off_budget_amount) from t_budget_expenses_project x 
	where x.yr = 2021 and x.project_for_type = 2 and x.active = 1
	and x.plan_id = a.plan_id
	and x.produce_id = a.produce_id
	and x.activity_id = a.activity_id
	and x.budget_type_id = a.budget_type_id
	and x.expenses_group_id = a.expenses_group_id
	and x.expenses_id = a.expenses_id
  ) as proj_estimate_off_budget_amount
from t_budget_expenses a where active = 1 and CAN_ADD_PROJECT = 1 and yr = 2021
group by plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id
) as final where estimate_off_budget_amount != proj_estimate_off_budget_amount;





select * from t_budget_expenses_project x
where x.yr = 2021 and x.project_for_type = 2-- and x.active = 1
	and x.produce_id = 3
	and x.activity_id = 1
	and x.budget_type_id = 1
	and x.expenses_group_id = 6
	and x.expenses_id = 17
;

select * from t_budget_expenses x
where x.yr = 2021 -- and x.active = 1
and x.plan_id = 3
	and x.produce_id = 3
	and x.activity_id = 1
	and x.budget_type_id = 1
	and x.expenses_group_id = 6
	and x.expenses_id = 17
;


select * from t_plan_configure where plan_id = 3;
select * from t_produce_configure where produce_id = 3;
select * from t_activity_configure where activity_id = 1;
select * from t_budget_type where budget_type_id = 1;
select * from t_expenses_group where expenses_group_id = 6;
select * from t_expenses_item where expenses_id = 18;