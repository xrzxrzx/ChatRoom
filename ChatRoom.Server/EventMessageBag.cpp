#include "EventMessageBag.h"

string EventMessageBag::ToJsonString() const
{
	json j;
	j["post_type"] = postType;
	j["data"] = data;
	return j.dump();
}
