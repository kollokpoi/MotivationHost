function getCookie(name) {
    var cookieValue = null
    if (document.cookie && document.cookie !== "") {
        var cookies = document.cookie.split(";")
        for (var i = 0; i < cookies.length; i++) {
            var cookie = cookies[i].trim()
            // Does this cookie string begin with the name we want?
            if (cookie.substring(0, name.length + 1) === name + "=") {
                cookieValue = decodeURIComponent(cookie.substring(name.length + 1))
                break
            }
        }
    }
    return cookieValue
}

function validatePositiveNum(value) {
    if (value.trim() == "") {
        return "Значение не может быть пустым!"
    }

    if (isNaN(value)) {
        return "Значение должно быть числом!"
    }

    if (value < 0) {
        return "Значение не может быть отрицательным!"
    }

    return null
}

function disableEdit() {
    var editButton = $("#enable-edit-btn")
    var editButtonIcon = editButton.children("i")
    editButtonIcon.removeClass("bi-x-lg").addClass("bi-pencil")
    editButton.contents().last().replaceWith("Редактировать")
    $(".delete-button").hide()
    $("#add-row").hide()
    $(".editable-click").editable("disable")
    $(".editable-error-block").remove()
    if ($("tbody tr").length > 2) {
        $(".empty-table-identificator").hide()
    }
}

function toggleEdit() {
    var editButton = $("#enable-edit-btn")
    var editButtonIcon = editButton.children("i")
    if (editButton.text().trim() == "Отмена") {
        editButtonIcon.removeClass("bi-x-lg").addClass("bi-pencil")
        editButton.contents().last().replaceWith("Редактировать")
        $(".delete-button").hide()
        $("#add-row").hide()
        $(".editable-click").editable("disable")
        $(".editable-error-block").remove()
        if ($("tbody tr").length <= 2) {
            $(".empty-table-identificator").show()
        }
        $("input").val("")
    } else {
        editButtonIcon.removeClass("bi-pencil").addClass("bi-x-lg")
        editButton.contents().last().replaceWith("Отмена")
        $(".delete-button").show()
        $("#add-row").show()
        $(".editable-click").editable("enable")
        $(".empty-table-identificator").hide()
    }
}

function handleError(error) {
    console.log(error)
    var errorMsg = JSON.parse(error.responseText).message
    $("#error-modal-body").text(errorMsg)
    $("#confirm-delete-modal").modal("hide")
    $("#error-modal").modal("show")
}

function makeEditableNumber(element, property, path) {
    $(element).editable({
        disabled: true,
        mode: "inline",
        validate: validatePositiveNum,
        url: path,
        ajaxOptions: {
            type: "POST",
            contentType: "application/json",
            headers: {
                Accept: "application/json",
            },
        },
        params: function (params) {
            var props = {}
            props["Id"] = params.pk
            props[property] = params.value

            var positionId = $(element).data("position-id")
            if (positionId) {
                props["PositionId"] = positionId
            }

            params = JSON.stringify(props)
            return params
        },
        error: handleError,
        success: function (response, newValue) {
            var updateTime = JSON.parse(response).updateTime
            var updateColumn = $(this).parent().next(".update-column")
            updateColumn.text(updateTime)
        },
    })
}

function makeEditableString(element, property, path) {
    $(element).editable({
        disabled: true,
        mode: "inline",
        step: "any",
        validate: function (value) {
            if (value.trim() == "") {
                return "Значение не может быть пустым!"
            }
        },
        url: path,
        ajaxOptions: {
            type: "POST",
            contentType: "application/json",
            headers: {
                Accept: "application/json",
            },
        },
        params: function (params) {
            var props = {}
            props["Id"] = params.pk
            props[property] = params.value

            var positionId = $(element).data("position-id")
            if (positionId) {
                props["PositionId"] = positionId
            }

            params = JSON.stringify(props)
            return params
        },
        error: handleError,
        success: function (response, newValue) {
            var updateTime = JSON.parse(response).updateTime
            var updateColumn = $(this).parent().parent().children(".update-column")
            updateColumn.text(updateTime)
        },
    })
}

function addModalDelete(message, path) {
    $(".delete-button").on("click", function (e) {
        e.preventDefault()
        var val = $(this).data("value")
        $("#delete-modal-title").text(message)
        $("#confirm-delete-modal").data("elem", this).modal("show")
    })

    $("#confirm-delete-button").on("click", function () {
        var elem = $("#confirm-delete-modal").data("elem")
        var id = $(elem).data("id")

        $.ajax({
            url: path,
            type: "DELETE",
            contentType: "application/json",
            headers: {
                Accept: "application/json",
            },
            success: function (result) {
                elem.parentNode.parentNode.remove()
                $("#confirm-delete-modal").modal("hide")
            },
            error: handleError,
            data: JSON.stringify({
                Id: id,
            }),
        })
    })
}

function addPosition() {
    $(".editable-error-block").remove()

    var name = $("#name-input").val()
    var salary = $("#salary-input").val()
    var isCheckFailed = false

    if (name.trim() == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#name-input"))
        isCheckFailed = true
    }

    var salaryCheck = validatePositiveNum(salary)
    if (salaryCheck != null) {
        $(`<div class="editable-error-block help-block">${salaryCheck}</div>`).insertAfter(
            $("#salary-input"),
        )
        isCheckFailed = true
    }

    if (isCheckFailed) return

    $.ajax({
        url: "/Positions/Create",
        type: "POST",
        contentType: "application/json",
        headers: {
            Accept: "application/json",
        },
        success: function (response) {
            var id = JSON.parse(response).id
            var newRow = $("<tr height='55px'>")
            var cols = ""
            cols += `<td class="col-2 ps-3"><a href="#" class="name-edit editable editable-click"  data-pk=${id} data-name="name" data-type="text" data-placement="right">${name}</a></td>`
            cols += `<td class="col-8 ps-3"><a href="#" class="salary-edit editable editable-click" data-pk=${id} data-name="salary" data-type="text" data-placement="right">${salary}</a></td>`
            cols += `<td class="text-center col-2">${new Date().toLocaleString()}</td>`
            cols += `<td style="min-width: 55px;" class="hidden-mobile"><button data-id=${id} class="btn btn-danger p-0 delete-button"><i class="bi bi-x-lg"></i></button></td>`
            newRow.append(cols)
            $("table").find("tr:last").prev().after(newRow)

            makeEditableString(".name-edit", "Name", "/Positions/Update")
            makeEditableNumber(".salary-edit", "Salary", "/Positions/Update")
            $(".editable-click").editable("enable")
            addModalDelete(
                "Вы действительно хотите удалить эту должность и связанные с ней квалификации и ранги?",
                "/Positions/Delete",
            )
            $(".empty-table-identificator").hide()

            $(".delete-button").on("click", function (e) {
                e.preventDefault()
                $("#delete-modal-title").text(
                    "Вы действительно хотите удалить эту должность и связанные с ней квалификации и ранги?",
                )
                $("#confirm-delete-modal").data("elem", this).modal("show")
            })
        },
        error: handleError,
        data: JSON.stringify({
            Name: name,
            Salary: salary,
        }),
    })
}

function addQualification() {
    $(".editable-error-block").remove()

    var name = $("#name-input").val()
    var points = $("#points-input").val()
    var isCheckFailed = false

    if (name.trim() == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#name-input"))
        isCheckFailed = true
    }

    var pointsCheck = validatePositiveNum(points)
    if (pointsCheck != null) {
        $(`<div class="editable-error-block help-block">${pointsCheck}</div>`).insertAfter(
            $("#points-input"),
        )
        isCheckFailed = true
    }

    if (isCheckFailed) return

    $.ajax({
        url: "/Qualifications/Create",
        type: "POST",
        contentType: "application/json",
        headers: {
            Accept: "application/json",
        },
        success: function (response) {
            $.ajax({
                type: "Get",
                url: "/Qualifications/Get?positionId=" + $("#position-select").val(),
                success: function (data) {
                    $("#table-body>.table-row").remove()
                    $(data).insertBefore("#table-body>#add-row")

                    makeEditableString(".name-edit", "Name", "/Qualifications/Update")
                    makeEditableNumber(".points-edit", "Points", "/Qualifications/Update")
                    addModalDelete(
                        "Вы действительно хотите удалить данную квалификацию?",
                        "/Qualifications/Delete",
                    )

                    $(".empty-table-identificator").hide()
                    $(".delete-button").show()
                    $("#add-row").show()
                    $(".editable-click").editable("enable")
                },
            })
        },
        error: handleError,
        data: JSON.stringify({
            Name: name,
            Points: points,
            PositionId: $("#position-select").val(),
        }),
    })
}

function addPenalty() {
    $(".editable-error-block").remove()

    var description = $("#description-input").val()
    var points = $("#points-input").val()
    var isCheckFailed = false

    if (description.trim() == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#description-input"))
        isCheckFailed = true
    }

    var pointsCheck = validatePositiveNum(points)
    if (pointsCheck != null) {
        $(`<div class="editable-error-block help-block">${pointsCheck}</div>`).insertAfter(
            $("#points-input"),
        )
        isCheckFailed = true
    }

    if (isCheckFailed) return

    $.ajax({
        url: "/Penalties/Create",
        type: "POST",
        contentType: "application/json",
        headers: {
            Accept: "application/json",
        },
        success: function (response) {
            $.ajax({
                type: "Get",
                url: "/Penalties/Get?positionId=" + $("#position-select").val(),
                success: function (data) {
                    $("#table-body>.table-row").remove()
                    $(data).insertBefore("#table-body>#add-row")

                    makeEditableString(".description-edit", "Description", "/Penalties/Update")
                    makeEditableNumber(".points-edit", "Points", "/Penalties/Update")
                    addModalDelete(
                        "Вы действительно хотите удалить данный штраф?",
                        "/Penalties/Delete",
                    )

                    $(".empty-table-identificator").hide()
                    $(".delete-button").show()
                    $("#add-row").show()
                    $(".editable-click").editable("enable")
                },
            })
        },
        error: handleError,
        data: JSON.stringify({
            Description: description,
            Points: points,
            PositionId: $("#position-select").val(),
        }),
    })
}

function addEmployeePenalty() {
    $(".editable-error-block").remove()

    var description = $("#description-input").val()
    var points = $("#points-input").val()
    var isCheckFailed = false

    if (description.trim() == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#description-input"))
        isCheckFailed = true
    }

    var pointsCheck = validatePositiveNum(points)
    if (pointsCheck != null) {
        $(`<div class="editable-error-block help-block">${pointsCheck}</div>`).insertAfter(
            $("#points-input"),
        )
        isCheckFailed = true
    }

    if (isCheckFailed) return

    $.ajax({
        url: "/Penalties/Create",
        type: "POST",
        contentType: "application/json",
        headers: {
            Accept: "application/json",
        },
        success: function (response) {
            $.ajax({
                type: "Get",
                url: "/Penalties/Get?positionId=" + $("#position-select").val(),
                success: function (data) {
                    $("#table-body>.table-row").remove()
                    $(data).insertBefore("#table-body>#add-row")

                    makeEditableString(".description-edit", "Description", "/Penalties/Update")
                    makeEditableNumber(".points-edit", "Points", "/Penalties/Update")
                    addModalDelete(
                        "Вы действительно хотите удалить данный штраф?",
                        "/Penalties/Delete",
                    )

                    $(".empty-table-identificator").hide()
                    $(".delete-button").show()
                    $("#add-row").show()
                    $(".editable-click").editable("enable")
                },
            })
        },
        error: handleError,
        data: JSON.stringify({
            Description: description,
            Points: points,
            PositionId: $("#position-select").val(),
        }),
    })
}

function addPointOfInterest() {
    $(".editable-error-block").remove()

    var name = $("#name-input").val()
    var latitude = $("#latitude-input").val()
    var longitude = $("#longitude-input").val()
    var isCheckFailed = false

    if (name.trim() == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#name-input"))
        isCheckFailed = true
    }

    if (latitude == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#latitude-input"))
        isCheckFailed = true
    } else if (latitude < -90 || latitude > 90) {
        $(
            '<div class="editable-error-block help-block">Значение широты не может за диапазоном [-90;90]!</div>',
        ).insertAfter($("#latitude-input"))
        isCheckFailed = true
    }

    if (longitude == "") {
        $(
            '<div class="editable-error-block help-block">Значение не может быть пустым!</div>',
        ).insertAfter($("#longitude-input"))
        isCheckFailed = true
    } else if (longitude < -180 || longitude > 180) {
        $(
            '<div class="editable-error-block help-block">Значение долготы не может за диапазоном [-180;180]!</div>',
        ).insertAfter($("#longitude-input"))
        isCheckFailed = true
    }

    if (isCheckFailed) return

    $.ajax({
        url: "/PointsOfInterest/Create",
        type: "POST",
        contentType: "application/json",
        headers: {
            Accept: "application/json",
        },
        success: function (response) {
            var id = JSON.parse(response).id
            var latitudeString = new Intl.NumberFormat("en", { minimumFractionDigits: 5 }).format(
                latitude,
            )
            var longitudeString = new Intl.NumberFormat("en", { minimumFractionDigits: 5 }).format(
                longitude,
            )
            var updateDateString = new Date().toLocaleString().replace(",", "")

            var newRow = $("<tr height='55px'>")
            var cols = ""
            cols += `<td class="col-4 ps-3"><a href="#" class="name-edit editable editable-click" data-pk=${id} data-name="name" data-type="text" data-placement="right">${name}</a></td>`
            cols += `<td class="col-3 ps-3"><a href="#" class="latitude-edit editable editable-click" data-pk=${id} data-name="latitude" data-type="number" step="any" min="-90" max="90" data-placement="right">${latitudeString}</a></td>`
            cols += `<td class="col-3 ps-3"><a href="#" class="longitude-edit editable editable-click" data-pk=${id} data-name="longitude" data-type="number" step="any" min="-180" max="180" data-placement="right">${longitudeString}</a></td>`
            cols += `<td class="text-center col-2">${updateDateString}</td>`
            cols += `<td style="min-width: 55px;" class="hidden-mobile"><button data-id=${id} class="btn btn-danger p-0 delete-button"><i class="bi bi-x-lg"></i></button></td>`
            newRow.append(cols)
            $("table").find("tr:last").prev().after(newRow)

            makeEditableString(".name-edit", "Name", "/PointsOfInterest/Update")
            makeEditableString(".latitude-edit", "Latitude", "/PointsOfInterest/Update")
            makeEditableString(".longitude-edit", "Longitude", "/PointsOfInterest/Update")
            $(".editable-click").editable("enable")
            addModalDelete("Вы действительно хотите удалить эту точку?", "/PointsOfInterest/Delete")
            $(".empty-table-identificator").hide()

            $(".delete-button").on("click", function (e) {
                e.preventDefault()
                $("#delete-modal-title").text("Вы действительно хотите удалить эту точку?")
                $("#confirm-delete-modal").data("elem", this).modal("show")
            })
        },
        error: handleError,
        data: JSON.stringify({
            Name: name,
            Latitude: latitude,
            Longitude: longitude,
        }),
    })
}

function changePositionInQualifications() {
    $.ajax({
        type: "Get",
        url: "/Qualifications/Get?positionId=" + $("#position-select").val(),
        success: function (data) {
            $("#table-body>.table-row").remove()
            $(data).insertBefore("#table-body>#add-row")

            makeEditableString(".name-edit", "Name", "/Qualifications/Update")
            makeEditableNumber(".points-edit", "Points", "/Qualifications/Update")
            addModalDelete(
                "Вы действительно хотите удалить данную квалификацию?",
                "/Qualifications/Delete",
            )

            disableEdit()
        },
    })
}

$("#Position").on("change", () => {
    $.ajax({
        type: "Get",
        url: "/Qualifications/GetJson?positionId=" + $("#Position").val(),
        success: function (data) {
            var qualifications = JSON.parse(data).qualifications

            $("#Qualification").empty()
            $.each(qualifications, function (key, value) {
                $("#Qualification").append($("<option></option>").attr("value", value).text(key))
            })
        },
    })

    $.ajax({
        type: "Get",
        url: "/Ranks/GetJson?positionId=" + $("#Position").val(),
        success: function (data) {
            var ranks = JSON.parse(data).ranks

            $("#Rank").empty()
            $.each(ranks, function (key, value) {
                $("#Rank").append($("<option></option>").attr("value", value).text(key))
            })
        },
    })
})
