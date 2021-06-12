
-- [จัดสรรงบ] เทียบงบประมาณ ระหว่าง budget_allocate_expenses vs budget_allocate
select dep_id, allocate_budget_amount
, 
(
	select sum(allocate_budget_amount) 
	from t_budget_allocate_expenses ex 
	where ex.dep_id = dep_allocate.dep_id
) as expenses_allocate_budget_amount
from t_budget_allocate dep_allocate
where exists(select * from t_department dep where dep.active = -1 and dep.dep_id = dep_allocate.dep_id)
;


-- [จัดสรรงบ] เทียบงบประมาณ ระหว่าง ประวัติ กับ ภาพรวมของหน่วยงาน
select dep_id, sum(ALLOCATE_BUDGET_AMOUNT) as his_allocate_budget
, (select allocate_budget_amount from t_budget_allocate x where x.dep_id = dep_allocate.dep_id) as dep_allocate_budget
from T_BUDGET_ALLOCATE_EXPENSES_HISTORY dep_allocate
where active = 1 and ADJUSTMENT_TYPE = 1
and yr = 2021
and budget_type = 1
-- and exists(select * from t_department dep where dep.dep_id = dep_allocate.dep_id and dep.active = 1)
group by dep_id;


-- [จัดสรรงบ] เทียบงบประมาณ ระหว่าง ประวัติ กับ ภาพรายการค่าใช้จ่าย
select dep_id, plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id,project_id, sum(allocate_budget_amount) as allocate_budget_amount
, (
select allocate_budget_amount
from t_budget_allocate_expenses x
where x.dep_id = dep_allocate.dep_id
and x.plan_id = dep_allocate.plan_id
and x.produce_id = dep_allocate.produce_id 
and x.activity_id = dep_allocate.activity_id 
and x.budget_type_id = dep_allocate.budget_type_id
and x.expenses_group_id = dep_allocate.expenses_group_id 
and x.expenses_id = dep_allocate.expenses_id
and (x.project_id is null and dep_allocate.project_id is null or x.project_id = dep_allocate.project_id)
) as net_allocate_budget_amount
from T_BUDGET_ALLOCATE_EXPENSES_HISTORY dep_allocate
where active = 1 and ADJUSTMENT_TYPE = 1
and yr = 2021
and budget_type = 1
and dep_id = 63
-- and exists(select * from t_department dep where dep.dep_id = dep_allocate.dep_id and dep.active = 1)
group by dep_id, plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id,project_id
;



-- [จัดสรรงบ] ประวัติการจัดสรรงบประมาณ
select dep_id, plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id,project_id, allocate_budget_amount
from T_BUDGET_ALLOCATE_EXPENSES_HISTORY dep_allocate
where active = 1 and ADJUSTMENT_TYPE = 2
and yr = 2021
and budget_type = 1
and dep_id = 63
and plan_id = 3 and produce_id = 3 and activity_id = 1 and budget_type_id = 1 and expenses_group_id = 6 and expenses_id = 17 and project_id = 1;
-- and exists(select * from t_department dep where dep.dep_id = dep_allocate.dep_id and dep.active = 1)
;




-- [จัดสรรงบ] งบประมาณ แต่ละรายการค่าใช้จ่าย 
select plan_id,produce_id,activity_id,budget_type_id,expenses_group_id,expenses_id,project_id, allocate_budget_amount
from t_budget_allocate_expenses
where dep_id = 63
and plan_id = 3 and produce_id = 3 and activity_id = 1 and budget_type_id = 1 and expenses_group_id = 6 and expenses_id IN(13,14);